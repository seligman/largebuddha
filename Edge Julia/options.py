#!/usr/bin/env python3

import os

OPTIONS = {
    "width": 3840,              # Width of the final output image
    "height": 2160,             # Height of the final output image
    "frame_rate": 60,           # The output frame rate, used to know how many dupe frames to add
    "gui_shrink": 4,            # Shrink the GUI view, but still track just as many pixels for output
    "show_gui": True,           # Should we show the GUI (env variable NO_GUI overrides)
    "multiproc": False,         # Draw multiple frames at once (disables show_gui)
    "multiproc_sync": False,    # Turn on multiproc, and call "sync.py up" every now and then
    "quick_mode": False,        # Disable alias, draw frames at 1/2 quality
    "view_only": False,         # Only view the main mandelbrot
    "save_results": True,       # Save all results as we go
    "add_extra_frames": True,   # Add extra frames to the start and end
    "draw_julias": True,        # Draw all Julia framesß
    "target_frames": 7000,      # Number of target frames after finding the border size
    "mand_loc": {"size": 11.0, "x": 4.5, "y": 1.5}, # Location of the main Mandelbrot on all Julia images
    "mand_iters": 100,          # Number of iterations for the main Mandelbrot
    "julia_iters": 500,         # Number of iterations for each Julia set
    "border_iter": 50,          # Number of iterations when searching for the border points
    "shrink": 1,                # Number to divide all width/height calls by
    "scan_size": 100_000,       # Number of points per unit when searching for the border
    "frame_spacing": 0.004      # Spacing, in Mandelbrot coords, between frames along the edge
}

# Some debug options to turn on easily
# OPTIONS["view_only"] = True
# OPTIONS["draw_julias"] = False
# OPTIONS["save_results"] = False
# OPTIONS["scan_size"] = 2000
# OPTIONS["quick_mode"] = True
# OPTIONS["shrink"] = 32
# OPTIONS["gui_shrink"] = 1
# OPTIONS["mand_loc"] = {"size": 5.0, "x": 0.75, "y": 0.0}
# OPTIONS["show_gui"] = False

if OPTIONS["shrink"] > 1:
    # If shrink is turned on, shrink down the image size
    OPTIONS["width"] //= OPTIONS["shrink"]
    OPTIONS["height"] //= OPTIONS["shrink"]
    OPTIONS["add_extra_frames"] = False

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

def show_flags():
    if "NO_GUI" in os.environ:
        print("NO_GUI option set")
    if "MULTIPROC" in os.environ:
        print("MULTIPROC option set")
    if "SYNCMODE" in os.environ:
        print("SYNCMODE option set")
    if "PROCS" in os.environ:
        print(f"PROCS option set to {os.environ['PROCS']}")
