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
# OPTIONS["no_alias"] = True
# OPTIONS["julia_iters"] = 250
# OPTIONS["shrink"] = 4
# OPTIONS["gui_shrink"] = 1
# OPTIONS["mand_loc"] = {"size": 5.0, "x": 0.75, "y": 0.0}
# OPTIONS["frame_spacing"] = 0.01
# OPTIONS["show_gui"] = False
# with open("data/edge_10000_e05x100.png.dat", "rb") as f:
#     temp = pickle.load(f)
#     while len(temp) >= 200: # 20_000:
#         temp = [temp[0]] + [x for i, x in enumerate(temp[1:-1]) if (i % 2) == 0] + [temp[-1]]
#     OPTIONS["saved_trail"] = temp

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
