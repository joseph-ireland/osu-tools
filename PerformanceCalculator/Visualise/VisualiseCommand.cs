using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Text;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace PerformanceCalculator.Visualise
{
    [Command(Name = "visualise", Description = "Compute values for adjustments.")]
    public class VisualiseCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [AllowedValues("FLOW_0", "SNAP_0", "FLOW_3", "SNAP_3", "FLOW_0_OLD", "SNAP_0_OLD", "FLOW_3_OLD", "SNAP_3_OLD")]
        [Argument(0, Name = "correction", Description = "Required. The name of the correction to visualise")]
        public string Name { get; }


        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-s|--start <start>", Description = "One for each input. The start of the range of values.")]
        public double[] Start { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-c|--count <count>", Description = "One for each input. Number of values in the range.")]
        public int[] Count { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-d|--step <step>", Description = "One for each input. Step size for the range of values.")]
        public double[] Step { get; }



        public override void Execute()
        {
            var fields = typeof(MultiL2NormCorrection).GetFields(BindingFlags.Public | BindingFlags.Static);
            var angleFields = typeof(AngleCorrection).GetFields(BindingFlags.Public | BindingFlags.Static);
            dynamic obj = fields.Union(angleFields).FirstOrDefault(x => x.Name == Name).GetValue(null);

            double[][][] data = new double[Count[0]][][];


            for (int i = 0; i < Count[0]; ++i)
            {
                data[i] = new double[Count[1]][];

                double z = Start[0] + i * Step[0];

                for (int j = 0; j < Count[1]; ++j)
                {
                    data[i][j] = new double[Count[2]];
                    double y = Start[2] + j * Step[1];

                    for (int k = 0; k < Count[2]; ++k)
                    {
                        double x = Start[2] + k * Step[2];

                        data[i][j][k] = obj.Evaluate(z, y, x);
                    }
                }
            }
            string json = JsonConvert.SerializeObject(data);
            if (OutputFile == null)
                Console.Write(json);
            else
                File.WriteAllText(OutputFile, json);

        }

    };
}
