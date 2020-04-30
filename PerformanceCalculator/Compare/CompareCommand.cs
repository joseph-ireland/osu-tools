using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator.Compare
{
    [Command(Name = "compare", Description = "Computes SR and pp for a set of scores defined in a json file")]
    public class CompareCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "path", Description = "Path of the .csv file")]
        public string ScoreSetsPath { get; }

        [UsedImplicitly]
        [Required]
        [Argument(1, Name = "set name", Description = "The name of the set of scores you want to compute pp for")]
        public string SetName { get; }

        private const string base_url = "https://osu.ppy.sh";


        public override void Execute()
        {
            var ruleset = new OsuRuleset();

            var allLines = File.ReadAllLines(ScoreSetsPath).Select(a => a.Split(','));
            var lines = allLines.Where(l => l[0] == SetName);

            var sw = new StringWriter();
            sw.WriteLine("ID, Beatmap, Mods, Combo, Max Combo, Accuracy, Aim pp, Tap pp, Acc pp, pp");

            foreach (var l in lines)
            {
                var s = new Score(l);
                var mods = s.Mods.ToArray();

                string beatmapID = s.ID;

                string cachePath = Path.Combine("cache", $"{beatmapID}.osu");

                if (!File.Exists(cachePath))
                {
                    Console.WriteLine($"Downloading {beatmapID}.osu...");
                    new FileWebRequest(cachePath, $"{base_url}/osu/{beatmapID}").Perform();
                }

                var workingBeatmap = new ProcessorWorkingBeatmap(cachePath);
                var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);

                var beatmapMaxCombo = getMaxCombo(beatmap);
                var maxCombo = (int)Math.Round(s.PercentCombo / 100 * beatmapMaxCombo);
                var accuracy = s.Accuracy;

                var statistics = generateHitResults(accuracy, beatmap, s.MissCount);

                var scoreInfo = new ScoreInfo
                {
                    Accuracy = accuracy,
                    MaxCombo = maxCombo,
                    Statistics = statistics,
                    Mods = mods,
                };
                var categoryAttribs = new Dictionary<string, double>();
                double pp = ruleset.CreatePerformanceCalculator(workingBeatmap, scoreInfo).Calculate(categoryAttribs);

                var resultLine = new List<string>()
                {
                    beatmap.BeatmapInfo.OnlineBeatmapID.ToString(),
                    beatmap.BeatmapInfo.ToString().Replace(",", ";"),
                    string.Join("", s.ModStrings),

                    maxCombo.ToString(),
                    beatmapMaxCombo.ToString(),
                    (accuracy * 100).ToString("F2"),
                    categoryAttribs["Aim"].ToString("F2"),
                    categoryAttribs["Tap"].ToString("F2"),
                    categoryAttribs["Accuracy"].ToString("F2"),
                    pp.ToString("F2")

                };

                sw.WriteLine(string.Join(", ", resultLine));

            }
            if (OutputFile == null)
                Console.Write(sw.ToString());
            else
                File.WriteAllText(OutputFile, sw.ToString());
            
            sw.Dispose();
        }

        private int getMaxCombo(IBeatmap beatmap) => beatmap.HitObjects.Count +
                                                     beatmap.HitObjects.OfType<Slider>().Sum(s => s.NestedHitObjects.Count - 1);


        private Dictionary<HitResult, int> generateHitResults(double accuracy, IBeatmap beatmap, int countMiss)
        {
            int countGreat;

            var totalResultCount = beatmap.HitObjects.Count;

            countGreat = totalResultCount - countMiss;
            int countGood = 0;
            int countMeh = 0;
            double newAcc = (double)((6 * countGreat) + (2 * countGood) + countMeh) / (6 * totalResultCount);
            if (newAcc > accuracy)
            {
                while (true)
                {
                    countGreat--;
                    countGood++;
                    newAcc = (double)((6 * countGreat) + (2 * countGood) + countMeh) / (6 * totalResultCount);
                    if (newAcc < accuracy)
                    {
                        countGood--;
                        countMeh++;
                        newAcc = (double)((6 * countGreat) + (2 * countGood) + countMeh) / (6 * totalResultCount);
                        if (newAcc < accuracy)
                        {
                            countGreat++;
                            countMeh--;
                            if (countGood == 0)
                            {
                                break;
                            }
                            else
                            {
                                while (true)
                                {
                                    if (countGood == 0)
                                    {
                                        break;
                                    }
                                    countGood--;
                                    countMeh++;
                                    newAcc = (double)((6 * countGreat) + (2 * countGood) + countMeh) / (6 * totalResultCount);
                                    if (newAcc < accuracy)
                                    {
                                        countGood++;
                                        countMeh--;
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }


            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countGreat },
                { HitResult.Good, countGood },
                { HitResult.Meh, countMeh },
                { HitResult.Miss, countMiss }
            };
        }

        protected void WriteAttribute(string name, string value) => Console.WriteLine($"{name.PadRight(15)}: {value}");

    }
}
    
