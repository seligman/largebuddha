#!/usr/bin/env python3

import os, pickle

OPTIONS = {
    "width": 3840,              # Width of the final output image
    "height": 2160,             # Height of the final output image
    "frame_rate": 60,           # The output frame rate, used to know how many dupe frames to add
    "gui_shrink": 4,            # Shrink the GUI view, but still track just as many pixels for output
    "show_gui": True,           # Should we show the GUI (env variable NO_GUI overrides)
    "multiproc": False,         # Draw multiple frames at once (disables show_gui)
    "multiproc_sync": False,    # Turn on multiproc, and call "sync.py up" every now and then
    "quick_mode": False,        # Disable alias, draw frames at 1/2 quality
    "no_alias": False,          # Disable alias, but draw frames at normal quality
    "view_only": False,         # Only view the main mandelbrot
    "save_results": True,       # Save all results as we go
    "add_extra_frames": True,   # Add extra frames to the start and end
    "draw_julias": True,        # Draw all Julia frames
    "save_edge": False,         # Draw the edge and save it as a graphics file
    "precise_point": True,      # Find the precise point after finding a target point
    "mand_loc": {"size": 11.0, "x": 4.5, "y": 1.5}, # Location of the main Mandelbrot on all Julia images
    "mand_iters": 100,          # Number of iterations for the main Mandelbrot
    "julia_iters": 250,         # Number of iterations for each Julia set
    "border_iter": 75,          # Number of iterations when searching for the border points
    "shrink": 1,                # Number to divide all width/height calls by
    "scan_size": 100_000,       # Number of points per unit when searching for the border
    "frame_spacing": 0.001      # Spacing, in Mandelbrot coords, between frames along the edge
}

# Some debug options to turn on easily
# OPTIONS["view_only"] = True
# OPTIONS["draw_julias"] = False
# OPTIONS["save_results"] = False
# OPTIONS["scan_size"] = 10_000
# OPTIONS["quick_mode"] = True
# OPTIONS["julia_iters"] = 250
# OPTIONS["mand_loc"] = {"size": 5.0, "x": 0.75, "y": 0.0}
# OPTIONS["frame_spacing"] = 0.01
# OPTIONS["show_gui"] = False

# OPTIONS["shrink"] = 4
# OPTIONS["gui_shrink"] = 1
# OPTIONS["no_alias"] = True

# --- Load and limit data file
if False:
    # edge_00022_e05x177
    # edge_00075_e06x177
    # edge_00100_e06x100
    OPTIONS["source_data_file"] = "edge_00100_e06x100"
    target_frames = 15_000
    source_fn = os.path.join("data", OPTIONS["source_data_file"] + ".png.dat")

    if "saved_trail" not in OPTIONS:
        def limit_trail(trail, spacing):
            import math
            ret = [trail[0]]
            for x, y in trail[1:-1]:
                if math.sqrt(((ret[-1][0] - x) ** 2) + ((ret[-1][1] - y) ** 2)) >= spacing:
                    ret.append((x, y))
            ret.append(trail[-1])
            return ret

        def find_nearest(target_length, fn):
            with open(fn, "rb") as f:
                temp = pickle.load(f)
            best_skip, best_trail = 0, temp
            for i in range(1, 20):
                for add in [-1, 1]:
                    test_skip = best_skip + add / (2 ** i)
                    test_trail = limit_trail(temp, test_skip)
                    if abs(target_length - len(test_trail)) < abs(target_length - len(best_trail)):
                        best_skip, best_trail = test_skip, test_trail
            return best_trail, best_skip, len(temp)

        OPTIONS["saved_trail"], best_skip, orig_frames = find_nearest(target_frames, source_fn)
        print(f'Using {OPTIONS["source_data_file"]}, with {len(OPTIONS["saved_trail"]):,} from {orig_frames:,} frames, and skip {best_skip}')

if "LOAD_TRAIL" in os.environ:
    with open(os.environ("LOAD_TRAIL"), "rb") as f:
        OPTIONS["saved_trail"] = pickle.load(f)

if "DRAW_EDGE" in os.environ:
    OPTIONS["border_iter"] = 3
    OPTIONS["save_edge"] = True
    OPTIONS["shrink"] = 4
    OPTIONS["quick_mode"] = True
    OPTIONS["gui_shrink"] = 1
    OPTIONS["mand_loc"] = {"size": 5.0, "x": 0.75, "y": 0.0}
    OPTIONS["scan_size"] = 2000

if "NO_GUI" in os.environ:
    # Allow an env variable to turn on GUI mode
    OPTIONS["show_gui"] = False

if "MULTIPROC" in os.environ or "SYNCMODE" in os.environ:
    # Allow an env variable to turn on multiproc or sync
    OPTIONS["show_gui"] = False
    OPTIONS["multiproc"] = True
    if "PROCS" in os.environ:
        OPTIONS["procs"] = int(os.environ["PROCS"])
    if "SYNCMODE" in os.environ:
        OPTIONS["multiproc_sync"] = True

if OPTIONS["shrink"] > 1:
    # If shrink is turned on, shrink down the image size
    OPTIONS["width"] //= OPTIONS["shrink"]
    OPTIONS["height"] //= OPTIONS["shrink"]
    OPTIONS["add_extra_frames"] = False

def show_flags():
    for arg in ["LOAD_TRAIL", "SAVE_TRAIL", "PROCS"]:
        if arg in os.environ:
            print(f"{arg} option set to {os.environ[arg]}")
    for arg in ["NO_GUI", "MULTIPROC", "SYNCMODE", "DRAW_EDGE"]:
        if arg in os.environ:
            print(f"{arg} option set")

if __name__ == "__main__":
    print("This module is not meant to be run directly")
