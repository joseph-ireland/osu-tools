#!/bin/python3


import matplotlib.pyplot as plt
import pandas as pd
import numpy as np
import sys
import argparse

parser=argparse.ArgumentParser(
    description="Read beatmap difficulty csv and plot it.",
    epilog="""
examples:
  dotnet -- plot csv BEATMAP.osu  | python plot.py
  python plot.py before.csv after.csv --labels before after
""",
    formatter_class=argparse.RawDescriptionHelpFormatter
    )

parser.add_argument("input", nargs="*", default=["-"], help="list of input files to read, default to stdin")
parser.add_argument("--index", action="store_true", help="use note index as x axis, default use time")
parser.add_argument("--labels", nargs="*", help="label for each input file, default uses file name")

args = parser.parse_args()

fig, axes = plt.subplots(nrows=2, ncols=1, sharex=True)

i=0;

for fname in args.input:
    with open(fname) if fname != "-" else sys.stdin as f:

        df = pd.read_csv(f)
        try:
            label_suffix = " - " + args.labels[i]
        except:
            label_suffix = "" if fname == "-" else " - " + fname

        if label_suffix:
            df.rename(columns=lambda x: x if x == "time" else x + label_suffix, inplace=True)

        if (args.index):
            df.plot(ax=axes[0], y=["aim"+label_suffix, "aimStars"+label_suffix])
            df.plot(ax=axes[1], y=["speed"+label_suffix, "speedStars"+label_suffix])
        else:
            df.plot(ax=axes[0], x="time", y=["aim"+label_suffix, "aimStars"+label_suffix])
            df.plot(ax=axes[1], x="time", y=["speed"+label_suffix, "speedStars"+label_suffix])

        i+=1

plt.show()
