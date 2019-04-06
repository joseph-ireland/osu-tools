using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;



namespace PerformanceCalculator.Plot
{
    [Command(Name = "plot", Description = "Outputs a plot of beatmap difficulty against time.")]
    class PlotCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [AllowedValues("csv")]
        [Argument(0, Name = "format", Description = "Required. Output format. Values: csv")]
        public string Format { get; }

        [UsedImplicitly]
        [Required, FileExists]
        [Argument(1, Name = "beatmap", Description = "Required. Beatmap file (.osu) to plot.")]
        public string Beatmap { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--m <mod>", Description = "One for each mod. The mods to compute the difficulty with."
                                                                                          + "Values: hr, dt, hd, fl, ez, 4k, 5k, etc...")]
        public string[] Mods { get; }


        public override void Execute()
        {
            var workingBeatmap = new ProcessorWorkingBeatmap(Beatmap);
            var ruleset = new OsuRuleset();
            var attributes = (OsuDifficultyAttributes)ruleset.CreateDifficultyCalculator(workingBeatmap).Calculate(getMods(ruleset).ToArray());

            if (Format == "csv")
            {
                csv(attributes.HitObjectDifficulties);
            }

        }

        private void csv(IEnumerable<OsuHitObjectDifficulty> difficulties)
        {
            TextWriter w;
            if (OutputFile != null)
                w = new StreamWriter(OutputFile);
            else
                w = Console.Out;

            w.WriteLine("time,aim,aimStars,speed,speedStars");
            foreach (var d in difficulties)
            {
                w.WriteLine($"{d.Time*0.001:0.000},{d.AimStars:0.000},{d.AimCumulativeStars:0.000},{d.SpeedStars:0.000},{d.SpeedCumulativeStars:0.000}");
            }
            w.Flush();
            if (w != Console.Out)
                w.Close();
        }



        private List<Mod> getMods(Ruleset ruleset)
        {
            var mods = new List<Mod>();
            if (Mods == null)
                return mods;

            var availableMods = ruleset.GetAllMods().ToList();
            foreach (var modString in Mods)
            {
                Mod newMod = availableMods.FirstOrDefault(m => string.Equals(m.Acronym, modString, StringComparison.CurrentCultureIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");
                mods.Add(newMod);
            }

            return mods;
        }
    }
}
