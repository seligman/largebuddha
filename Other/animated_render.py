#!/usr/bin/env python3

import numpy as np
from PIL import Image
import random
import math
import os

r"""

To render the final collection of images to a video file, use something like

ffmpeg -hide_banner -f image2 -framerate 30 -i "frame_%05d.png" "animated.mp4"

"""

# The options that control the quality and size
size = 500              # Size in pixels
sampling = 15           # Number of sub-pixels to calc per pixel, this is squared per pixel
rows_per_frame = 2      # Number of rows to draw before saving a frame

# Remove any old images that exist
for cur in os.listdir("."):
    if cur.startswith("frame_") and cur.endswith(".png"):
        os.unlink(cur)

# The core mandelbrot formula
mandel = lambda c,z: z*z+c

# Keep one work image around through the run of this
im = Image.new('RGB',(size,size))
# Keep a height map.  Actually, three of em, one for each of RGB
hits = []
for _ in range(size*size):
    hits.append([0,0,0])
# PIL helper to let us modify pixels directly
pixels = im.load()
# The frame we're currently on
frame = 0
# Use to skip output of frames, once it hits rows_per_frame, we draw the frame
skip = 0
# Just track if we hit _any_ pixels at all, if we didn't don't bother saving the frame
pixels_hit = 0

def save_frame(info, shutter, force=False, repeat=1):
    # This helper is responsible for taking the height map and turning it
    # into an image.  Shutter should be an range of "x" value of where to 
    # draw a red line, representing the current "shutter", or bit we just 
    # worked on
    if pixels_hit > 0 or force:
        # Only bother if there's something we did

        for x in range(size):
            for y in range(size):
                # So this takes each item from the height map and normalizes it
                # These values are taken from a previous run at these settings
                # to get an idea of what levels to use,  This is the first
                # triple that's output on each row, note it's reversed here
                color = ( #                               vvvvv
                    int(max(0, min(1, hits[x+y*size][2] /  2937)) * 255),
                    int(max(0, min(1, hits[x+y*size][1] /  1702)) * 255),
                    int(max(0, min(1, hits[x+y*size][0] /   731)) * 255),
                )
                pixels[x,y] = color
                if shutter is not None:
                    # Draw the shutter if we're told to
                    if x >= shutter[0] and x < shutter[1]:
                        pixels[x,y] = (255, color[1], color[2])
        # Also show the ideal levels, and the real top level.  The idea 
        # here is to pick a level that's near the max, but leave some
        # room for exponential growth of a handful of pixels.  99.9%
        # here is arbitrary, but it seems to look good enough.
        vals = [
            list(sorted([x[0] for x in hits]))[int(size * size * 0.999)],
            list(sorted([x[1] for x in hits]))[int(size * size * 0.999)],
            list(sorted([x[2] for x in hits]))[int(size * size * 0.999)],
        ]
        # And the real max values, just informational
        vals_max = [
            max(sorted([x[0] for x in hits])),
            max(sorted([x[1] for x in hits])),
            max(sorted([x[2] for x in hits])),
        ]
        for _ in range(repeat):
            # And increment the frame number, and save things, repeating
            # the save if we're told to
            global frame
            frame += 1
            fn = "frame_%05d.png" % (frame,)
            im.save(fn)
        print(fn, info, vals, vals_max)

# Run through each pixel
last_shutter = None
for c_i in np.linspace(-2, 2, size*sampling):
    if last_shutter is None:
        last_shutter = c_i
    pixels_hit = 0
    for c_r in np.linspace(-2, 2, size*sampling):
        trail = []
        in_period = False
        # c here has some random fuzz added to it to prevent some moire artifacts
        c = complex(c_r + random.random() * (3.0 / (size * sampling)), c_i + random.random() * (3.0 / (size * sampling)))
        z = c
        if not in_period:
            # First off see if we're in one of the well-known areas in the m-set
            # if we are, we don't bother calculating the iterations, since we won't
            # escape the m-set
            p = math.sqrt(((c.real - 0.25) * (c.real - 0.25)) + (c.imag * c.imag))
            if z.real < p - 2 * p * p + 0.25:
                in_period = True
        
        if not in_period:
            # The other well-known area
            if ((c.real + 1) * (c.real + 1)) + c.imag * c.imag < 0.0625:
                in_period = True

        if not in_period:
            # Ok, this might be outside the m-set, go ahead and calculate
            # some iterations.  Hard coded here, if this changes, we'll 
            # need to change the color values above
            in_period = True
            for i in range(5000):
                z = mandel(c, z)
                if abs(z) > 2: 
                    in_period = False
                    break
                trail.append(z)

        if not in_period:
            # And after working through the iterations, we still escaped
            # so, we need to plot these on our height maps
            for z in trail:
                # Find the display x/y values from the complex number
                # in the trail
                y = int(((z.real + 2) / 3) * (size-1) + 0.5)
                x = int(((z.imag + 1.5) / 3) * (size-1) + 0.5)
                if x >= 0 and y >= 0 and x < size and y < size:
                    # If the trail was larger than this number of items it means 
                    # a calculation at that level would have considered it inside 
                    # the m-set, so we only use trails that are this small
                    if len(trail) <= 50:
                        hits[x + y * size][0] += 1
                    # Same story for the second height map
                    if len(trail) <= 500:
                        hits[x + y * size][1] += 1
                    # The third one is based off our total trail size, so anything
                    # that didn't escape is valid here
                    hits[x + y * size][2] += 1
                    # Just note we hit a pixel so we skip frames that did nothing
                    pixels_hit += 1

    # And dump out the frame if we've done enough work
    skip += 1
    if skip == sampling * rows_per_frame:
        skip = 0
        # Figure out where the "shutter" should be
        shutter = [
            int(((last_shutter + 1.5) / 3) * (size-1) + 0.5),
            int(((c_i + 1.5) / 3) * (size-1) + 0.5),
        ]
        last_shutter = None
        save_frame("%7.4f x %7.4f" % (c_r, c_i), shutter, force=shutter[1]>= 0 and shutter[0]<size)

# And force the final frame, we want to save it even if nothing's
# different from the last one since there won't be a shutter, 
# also repeat it a few times to give us something to "hit" in
# case the video loops
save_frame("final", None, force=True, repeat=15)
