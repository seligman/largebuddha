#!/usr/bin/env python3

import os

OPTIONS = {
    "width": 1920,              # Width of the final output image
    "height": 1080,             # Height of the final output image
    "show_gui": True,           # Should we show the GUI (env variable NO_GUI overrides)
    "multiproc": False,         # Draw multiple frames at once (disables show_gui)
    "multiproc_sync": False,    # Turn on multiproc, and call "sync.py up" every now and then
    "quick_mode": False,        # Disable alias, draw frames at 1/2 quality
    "view_only": False,         # Only view the main mandelbrot
    "save_results": True,       # Save all results as we go
    "add_extra_frames": True,   # Add extra frames to the start and end
    "dump_different_trails": False, # When calculating the border, dump out some stats on how to shrink it
    "draw_julias": True,        # Draw all Julia frames
    "trail_length_target": 400, # 1/x of the length between points along the edge 
    "mand_loc": {"size": 11.0, "x": 4.5, "y": 1.5}, # Location of the main Mandelbrot on all Julia images
    "mand_iters": 100,          # Number of iterations for the main Mandelbrot
    "julia_iters": 250,         # Number of iterations for each Julia set
    "border_iter": 500,         # Number of iterations when searching for the border points
    "shrink": 1,                # Number to divide all width/height calls by
    "scan_size": 100000,        # Number of points per unit when searching for the border
}

# Some debug options to turn on easily
# OPTIONS["dump_different_trails"] = True
# OPTIONS["view_only"] = True
# OPTIONS["draw_julias"] = False
# OPTIONS["save_results"] = False
# OPTIONS["scan_size"] = 5000
# OPTIONS["quick_mode"] = True
# OPTIONS["shrink"] = 2
# OPTIONS["mand_loc"] = {"size": 5.0, "x": 0.75, "y": 0.0}

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
    if "SYNCMODE" in os.environ:
        OPTIONS["multiproc_sync"] = True
