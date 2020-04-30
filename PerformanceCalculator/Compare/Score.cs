using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculator.Compare
{
    class Score
    {
        public string ID;
        public List<string> ModStrings = new List<string>();
        public List<Mod> Mods;
        public double PercentCombo;
        public double Accuracy;
        public int MissCount;

        public Score(string[] args)
        {
            ID = args[1];

            ModStrings = args[2].Split(" ").ToList();
            Mods = getMods();

            PercentCombo = double.Parse(args[3]);
            Accuracy = double.Parse(args[4]) / 100;
            MissCount = int.Parse(args[5]);

        }

        private List<Mod> getMods()
        {
            var mods = new List<Mod>();

            if (ModStrings[0] == "")
            {
                return mods;
            }

            var availableMods = new OsuRuleset().GetAllMods().ToList();

            foreach (var modString in ModStrings)
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
