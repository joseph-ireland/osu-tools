import sys
import numpy as np
import matplotlib.pyplot as plt


iter_colors = iter(["#0ED4F9","#83BAFA","#BF9CE1","#E6759F","#D66759"])


if __name__ == "__main__":

    if len(sys.argv) >= 2:
        name = sys.argv[1]
    else:
        name = ""

    a = np.transpose(np.loadtxt(name))
    
    # times, IPs_raw, IPs, miss_probs, aim_strains = a[0], a[1], a[2], a[3], a[4]
    times, IPs_raw, IPs, miss_probs = a[0], a[1], a[2], a[3]

    fig, axarr = plt.subplots(2, sharex=True, figsize=[12,6])
    
    axarr[0].plot(times, IPs, '.', alpha=0.8)
    axarr[0].vlines(times, IPs_raw, IPs, colors=(1.0,0.5,0.5,0.8), linewidths=1)
    # axarr[0].plot(times, aim_strains, '.-', linewidth=0.5, markersize=2, alpha=0.5)
    
    axarr[0].set_ylabel("Index of Performance (bits/s)")

    axarr[1].plot(times, miss_probs, '.', alpha=0.8)
    axarr[1].set_xlabel("Time (s)")
    axarr[1].set_ylabel("Miss Probability")
    plt.savefig("cache/graph.png")
    plt.show()

