// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Scoring;

namespace PerformanceCalculator.Profile
{
    /// <summary>
    /// Holds the live pp value, beatmap name, and mods for a user play.
    /// </summary>
    public class UserPlayInfo
    {
        public double LocalPP;
        public double PerfectPP;
        public double FcPP;
        public double LivePP;

        public BeatmapInfo Beatmap;
        public double MapCombo;

        public string Mods;
        public ScoreInfo ScoreInfo;
    }
}
