// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculator.Profile
{
    [Command(Name = "profile", Description = "Computes the total performance (pp) of a profile.")]
    public class ProfileCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "user", Description = "User ID is preferred, but username should also work.")]
        public string ProfileName { get; }

        [UsedImplicitly]
        [Required]
        [Argument(1, Name = "api key", Description = "API Key, which you can get from here: https://osu.ppy.sh/p/api")]
        public string Key { get; }

        [UsedImplicitly]
        [Option(Template = "-r|--ruleset:<ruleset-id>", Description = "The ruleset to compute the profile for. 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania. Defaults to osu!.")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }

        private const string base_url = "https://osu.ppy.sh";

        public override void Execute()
        {
            var displayPlays = new List<UserPlayInfo>();

            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? 0);

            Console.WriteLine("Getting user data...");
            dynamic userData = getJsonFromApi($"get_user?k={Key}&u={ProfileName}&m={Ruleset}")[0];

            Console.WriteLine("Getting user top scores...");
            foreach (var play in getJsonFromApi($"get_user_best?k={Key}&u={ProfileName}&m={Ruleset}&limit=100"))
            {
                string beatmapID = play.beatmap_id;

                string cachePath = Path.Combine("cache", $"{beatmapID}.osu");
                if (!File.Exists(cachePath))
                {
                    Console.WriteLine($"Downloading {beatmapID}.osu...");
                    new FileWebRequest(cachePath, $"{base_url}/osu/{beatmapID}").Perform();
                }

                Mod[] mods = ruleset.ConvertLegacyMods((LegacyMods)play.enabled_mods).ToArray();

                var working = new ProcessorWorkingBeatmap(cachePath, (int)play.beatmap_id) { Mods = { Value = mods } };
                var beatmap = working.GetPlayableBeatmap(ruleset.RulesetInfo);
                var mapCombo = beatmap.HitObjects.Count + beatmap.HitObjects.OfType<Slider>().Sum(s => s.NestedHitObjects.Count - 1);

                var score = new ProcessorScoreParser(working).Parse(new ScoreInfo
                {
                    Ruleset = ruleset.RulesetInfo,
                    MaxCombo = play.maxcombo,
                    Mods = mods,
                    Statistics = new Dictionary<HitResult, int>
                    {
                        { HitResult.Perfect, (int)play.countgeki },
                        { HitResult.Great, (int)play.count300 },
                        { HitResult.Good, (int)play.count100 },
                        { HitResult.Ok, (int)play.countkatu },
                        { HitResult.Meh, (int)play.count50 },
                        { HitResult.Miss, (int)play.countmiss }
                    }
                });

                var fcScore = new ProcessorScoreParser(working).Parse(new ScoreInfo
                {
                    Ruleset = ruleset.RulesetInfo,
                    MaxCombo = mapCombo,
                    Mods = mods,
                    Statistics = new Dictionary<HitResult, int>
                    {
                        { HitResult.Perfect, (int)play.countgeki },
                        { HitResult.Great, (int)play.count300 },
                        { HitResult.Good, (int)play.count100+(int)play.countmiss },
                        { HitResult.Ok, (int)play.countkatu },
                        { HitResult.Meh, (int)play.count50 },
                        { HitResult.Miss, 0 }
                    }
                });



                var perfectScore = new ProcessorScoreParser(working).Parse(new ScoreInfo
                {
                    Ruleset = ruleset.RulesetInfo,
                    MaxCombo = mapCombo,
                    Mods = mods,
                    Statistics = new Dictionary<HitResult, int>
                    {
                        { HitResult.Perfect, 0 },
                        { HitResult.Great, beatmap.HitObjects.Count },
                        { HitResult.Good, 0 },
                        { HitResult.Ok, 0 },
                        { HitResult.Meh, 0 },
                        { HitResult.Miss, 0 }
                    }
                });




                var thisPlay = new UserPlayInfo
                {
                    Beatmap = working.BeatmapInfo,
                    LocalPP = ruleset.CreatePerformanceCalculator(working, score.ScoreInfo).Calculate(),
                    FcPP = ruleset.CreatePerformanceCalculator(working, fcScore.ScoreInfo).Calculate(),
                    PerfectPP = ruleset.CreatePerformanceCalculator(working, perfectScore.ScoreInfo).Calculate(),
                    LivePP = play.pp,
                    Mods = mods.Length > 0 ? mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}") : "None",
                    ScoreInfo = score.ScoreInfo,
                    MapCombo = mapCombo
                };

                displayPlays.Add(thisPlay);
            }

            var localOrdered = displayPlays.OrderByDescending(p => p.LocalPP).ToList();
            var liveOrdered = displayPlays.OrderByDescending(p => p.LivePP).ToList();

            int index = 0;
            double totalLocalPP = localOrdered.Sum(play => Math.Pow(0.95, index++) * play.LocalPP);
            double totalLivePP = userData.pp_raw;

            index = 0;
            double nonBonusLivePP = liveOrdered.Sum(play => Math.Pow(0.95, index++) * play.LivePP);

            //todo: implement properly. this is pretty damn wrong.
            var playcountBonusPP = (totalLivePP - nonBonusLivePP);
            totalLocalPP += playcountBonusPP;
            double totalDiffPP = totalLocalPP - totalLivePP;

            OutputDocument(new Document(
                new Span($"User:     {userData.username}"), "\n",
                new Span($"Live PP:  {totalLivePP:F1} (including {playcountBonusPP:F1}pp from playcount)"), "\n",
                new Span($"Local PP: {totalLocalPP:F1} ({totalDiffPP:+0.0;-0.0;-})"), "\n",
                new Grid
                {
                    Columns = {
                        new Column {Width = GridLength.Auto, MaxWidth = 100 },
                        GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto,
                        GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto,
                    },
                    Children =
                    {
                        new Cell("beatmap"),
                        new Cell("mods"),
                        new Cell("live pp"),
                        new Cell("local pp"),
                        new Cell("FC PP"),
                        new Cell("SS PP"),
                        new Cell("pp change"),
                        new Cell("position change"),
                        new Cell("combo"),
                        new Cell("misses"),
                        new Cell("acc"),

                        localOrdered.Select(item => new[]
                        {
                            new Cell($"{item.Beatmap.OnlineBeatmapID} - {item.Beatmap}"),
                            new Cell($"{item.Mods}") { Align = Align.Center },
                            new Cell($"{item.LivePP:F1}") { Align = Align.Right },
                            new Cell($"{item.LocalPP:F1}") { Align = Align.Right },
                            new Cell($"{item.FcPP:F1}") { Align = Align.Right },
                            new Cell($"{item.PerfectPP:F1}") { Align = Align.Right },
                            new Cell($"{item.LocalPP - item.LivePP:F1}") { Align = Align.Right },
                            new Cell($"{liveOrdered.IndexOf(item) - localOrdered.IndexOf(item):+0;-0;-}") { Align = Align.Center },
                            new Cell($"{item.ScoreInfo.MaxCombo}/{item.MapCombo}"){ Align = Align.Center },
                            new Cell($"{item.ScoreInfo.Statistics[HitResult.Miss]}"){ Align = Align.Center },
                            new Cell($"{item.ScoreInfo.Accuracy * 100:F1}%"){ Align = Align.Right },
                        })
                    }
                }
            ));
        }

        private dynamic getJsonFromApi(string request)
        {
            var req = new JsonWebRequest<dynamic>($"{base_url}/api/{request}");
            req.Perform();
            return req.ResponseObject;
        }
    }
}
