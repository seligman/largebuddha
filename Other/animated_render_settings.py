#!/usr/bin/env python3

# Turn this on to use multiple cores
USE_MULTIPROCESSING = False

# This shows the "ghost" Mandelbrot image before the 
# main image is drawn
SHOW_GHOST = False

# The different settings:
# SIZE = The square size of the image to render
# OFF_X, OFF_Y = The offset of the image. 
# The image width will be SIZE + OFF_X * 2
# SAMPLING = The number of sampling per side per pixel
# ROWS_PER_FRAME = The number of pixels to use per each frame
# LEVELS = The levels to use for the height map.  Ideally this 
# is from a previous run with the above values already set

# A shutter can shrink if it hits this number of pixels
# drawn.  The idea is to use the final value output from
# a test render here, and in this case, target around 900
# frames for a 30 second video.  The shutter will normal
# move at a rate to finish in 150 frames, but can shrink
# if something interesting is happening.
TARGET_PIXS_FRAME = None


# ----- A Preview level ---------------------------------------------
# SIZE = 500
# OFF_X, OFF_Y = 0, 0
# SAMPLING = 2
# ROWS_PER_FRAME = 8
# LEVELS = [[4, 4, 4], [26, 58, 108]] # verified

# ----- A Preview level, with a 16:9 display ------------------------
# SIZE = 540
# OFF_X, OFF_Y = 210, 0
# SAMPLING = 2
# ROWS_PER_FRAME = 8
# LEVELS = [[4, 5, 5], [27, 58, 112]]  # verified

# ----- A normal level ----------------------------------------------
SIZE = 500
OFF_X, OFF_Y = 0, 0
SAMPLING = 15
ROWS_PER_FRAME = 4
LEVELS = [[232, 241, 241], [1289, 2991, 5138]]  # verified

# ----- A high quality mode, at 720p --------------------------------
# SIZE = 720
# SAMPLING = 20
# OFF_X, OFF_Y = 280, 0
# ROWS_PER_FRAME = 2
# LEVELS = [[427, 442, 444], [2310, 5337, 9132]] # verified

# ----- A high quality mode, at 1080p -------------------------------
# SIZE = 1080
# SAMPLING = 20
# OFF_X, OFF_Y = 420, 0
# ROWS_PER_FRAME = 2
# LEVELS = [[427, 443, 444], [2322, 5358, 9210]] # verified

# ----- A very high quality mode, at 4k -----------------------------
# SIZE = 2160
# SAMPLING = 20
# OFF_X, OFF_Y = 840, 0   # Offset to the image
# ROWS_PER_FRAME = 4
# LEVELS = [[435, 451, 452], [2334, 5385, 9292]]

if __name__ == "__main__":
    print("This module is not meant to be run directly")
