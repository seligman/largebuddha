#!/usr/bin/env python3

import numpy as np
from PIL import Image
import random
import math
import os
from datetime import datetime
import multiprocessing
from collections import defaultdict
import sys
if sys.version_info >= (3, 11): from datetime import UTC
else: import datetime as datetime_fix; UTC=datetime_fix.timezone.utc

r"""

# To render the final collection of images to a video file, use something like

ffmpeg -hide_banner -f image2 -framerate 30 -i "frame_%05d.png" "animated.mp4"

# To render a high quality 4k version, use something like

ffmpeg -y -hide_banner -f image2 -framerate 30 -i "frame_%05d.png" -c:v libx265 -preset veryslow -b:v 100000k -x265-params pass=1 animation_4k.mkv
ffmpeg -y -hide_banner -f image2 -framerate 30 -i "frame_%05d.png" -c:v libx265 -preset veryslow -b:v 100000k -x265-params pass=2 animation_4k.mkv

# To view it without rendering, use:

ffplay -hide_banner -f image2 -framerate 30 -i "frame_%05d.png"

"""

# The settings are stored in a seperate file for convenience
# Treat all of the settings like a global variable
from animated_render_settings import *

# A class to save the current frame
class Saver:
    def __init__(self):
        # Just note the start time for the ticker in the output
        self.started = datetime.now(UTC).replace(tzinfo=None)
        # Keep one work image around through the run of this
        self.im = Image.new('RGB',(SIZE+OFF_X*2,SIZE+OFF_Y*2))
        # PIL helper to let us modify pixels directly
        self.pixels = self.im.load()
        # The frame we're currently on
        self.frame = 0
        # The last shutter that we drew, if any
        self.last_shutter = None
        # Used to draw the shadow mandelbrot
        self.mandel = [0] * (SIZE * SIZE)
        # Where to start drawing the ghost
        self.ghost_x = 0

    def save_frame(self, hits, info, shutter, draw=False, repeat=1):
        # This helper is responsible for taking the height map and turning it
        # into an image.  Shutter should be an range of "x" value of where to 
        # draw a red line, representing the current "shutter", or bit we just 
        # worked on
        fn = "<none>"
        vals_a, vals_b = None, None

        # Erase the last shutter, and draw the next in case either is outside 
        # of the main square for the fractal
        if self.last_shutter is not None:
            for y in range(SIZE):
                for x in range(self.last_shutter[0], self.last_shutter[1]):
                    if x + OFF_X >= 0 and x + OFF_X < SIZE + OFF_X * 2:
                        self.pixels[x + OFF_X, y] = (0, 0, 0)
        if shutter is not None:
            self.ghost_x = shutter[1]
            for y in range(SIZE):
                for x in range(shutter[0], shutter[1]):
                    if x + OFF_X >= 0 and x + OFF_X < SIZE + OFF_X * 2:
                        self.pixels[x + OFF_X, y] = (255, 0, 0)
        self.last_shutter = shutter

        if draw:
            for x in range(SIZE):
                for y in range(SIZE):
                    # So this takes each item from the height map and normalizes it
                    # These values are taken from a previous run at these settings
                    # to get an idea of what levels to use
                    color = (
                        int(max(0, min(1, (hits[x+y*SIZE][2] - LEVELS[0][2]) / (LEVELS[1][2] - LEVELS[0][2]))) * 255),
                        int(max(0, min(1, (hits[x+y*SIZE][1] - LEVELS[0][1]) / (LEVELS[1][1] - LEVELS[0][1]))) * 255),
                        int(max(0, min(1, (hits[x+y*SIZE][0] - LEVELS[0][0]) / (LEVELS[1][0] - LEVELS[0][0]))) * 255),
                    )

                    if x > self.ghost_x:
                        # As we draw, draw a ghost of the mandelbrot
                        color = (
                            min(255, int(color[1] + self.mandel[x + y * SIZE] * 35)),
                            min(255, int(color[0] + self.mandel[x + y * SIZE] * 35)),
                            min(255, int(color[2] + self.mandel[x + y * SIZE] * 35)),
                        )

                    self.pixels[x+OFF_X,y+OFF_Y] = color
                    if shutter is not None:
                        # Draw the shutter if we're told to
                        if x >= shutter[0] and x <= shutter[1]:
                            self.pixels[x+OFF_X,y+OFF_Y] = (255, color[1], color[2])
            # Also show the ideal levels.  The idea here is to pick a level that's 
            # near the min and max, but leave some room for exponential growth of 
            # a handful of pixels and to get to true black at the bottom.  40% to 
            # 99.9% here is arbitrary, but it seems to look good enough.
            vals_a = [
                list(sorted([x[0] for x in hits]))[int(SIZE * SIZE * 0.4)],
                list(sorted([x[1] for x in hits]))[int(SIZE * SIZE * 0.4)],
                list(sorted([x[2] for x in hits]))[int(SIZE * SIZE * 0.4)],
            ]
            vals_b = [
                list(sorted([x[0] for x in hits]))[int(SIZE * SIZE * 0.999)],
                list(sorted([x[1] for x in hits]))[int(SIZE * SIZE * 0.999)],
                list(sorted([x[2] for x in hits]))[int(SIZE * SIZE * 0.999)],
            ]
            for _ in range(repeat):
                # And increment the frame number, and save things, repeating
                # the save if we're told to
                self.frame += 1
                fn = "frame_%05d.png" % (self.frame,)
                self.im.save(os.path.join("images", fn))

        # Dump out some stats
        print("%9.2f: %-16s %18s [%s, %s]" % (
            (datetime.now(UTC).replace(tzinfo=None) - self.started).total_seconds(),
            fn, 
            str(info), 
            str(vals_a), str(vals_b),
        ))

def worker(worker_id, queue, queue_done):
    batch = [defaultdict(int), defaultdict(int), defaultdict(int)]
    while True:
        job = queue.get()
        if job[0] == "close":
            break
        elif job[0] == 'work':
            for cur in job[1]:
                calc_mandel(batch, cur[0], cur[1])
        elif job[0] == 'flush':
            for i in range(3):
                batch[i] = sorted([(x, batch[i][x]) for x in batch[i]])
            queue_done.put(batch)
            batch = [defaultdict(int), defaultdict(int), defaultdict(int)]
        else:
            raise Exception()

# Main worker to calculate one point in the buddhabrot set
def calc_mandel(batch, c_r, c_i, was_in_period=None):
    trail = []
    in_period = False
    # c here has some random fuzz added to it to prevent some moire artifacts
    c = complex(c_r + random.random() * (3.0 / (SIZE * SAMPLING)), c_i + random.random() * (3.0 / (SIZE * SAMPLING)))
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
        for _ in range(5000):
            z = z * z + c
            if abs(z) > 2: 
                in_period = False
                break
            if batch is not None:
                trail.append(z)

    if not in_period and batch is not None:
        # And after working through the iterations, we still escaped
        # so, we need to plot these on our height maps
        for z in trail:
            # Find the display x/y values from the complex number
            # in the trail.  The offset by 100000 is to prevent an 
            # artifact that occurs at zero and rounding
            y = int(((z.real + 2) / 3) * SIZE + 100000.5) - 100000
            x = int(((z.imag + 1.5) / 3) * SIZE + 100000.5) - 100000
            if x >= 0 and y >= 0 and x < SIZE and y < SIZE:
                # If the trail was larger than this number of items it means 
                # a calculation at that level would have considered it inside 
                # the m-set, so we only use trails that are this small
                if len(trail) <= 50:
                    batch[0][x + y * SIZE] += 1
                # Same story for the second height map
                if len(trail) <= 500:
                    batch[1][x + y * SIZE] += 1
                # The third one is based off our total trail size, so anything
                # that didn't escape is valid here
                batch[2][x + y * SIZE] += 1
    
    if was_in_period is not None:
        was_in_period[0] = in_period


def shuffle_split(values, n):
    for i in range(n):
        temp = [values[x] for x in range(len(values)) if x % n == i]
        random.shuffle(temp)
        yield temp


def ghost_worker(job):
    c_r, c_i, x, y = job
    was_in_period = [False]
    calc_mandel(None, c_r, c_i, was_in_period=was_in_period)
    if was_in_period[0]:
        return (x // SAMPLING + (y // SAMPLING) * SIZE, 1)
    else:
        return None


def main():
    if not os.path.isdir("images"):
        os.mkdir("images")

    # Remove any old images that exist
    for cur in os.listdir("images"):
        if cur.startswith("frame_") and cur.endswith(".png"):
            os.unlink(os.path.join("images", cur))

    # This is the helper to save a frame, it has some simple state, so it's
    # implemented as a class
    saver = Saver()

    # Use to skip output of frames, once it hits ROWS_PER_FRAME, we draw the frame
    skip = 0

    # Prepare the shadow mandelbrot first
    if SHOW_GHOST:
        for x in range(0, SIZE * SAMPLING):
            c_i = ((x / (SIZE * SAMPLING)) * 3.0) - 1.5
            if USE_MULTIPROCESSING:
                todo = []
                for y in range(0, SIZE * SAMPLING):
                    c_r = ((y / (SIZE * SAMPLING)) * 3.0) - 2.0
                    todo.append((c_r, c_i, x, y))
                with multiprocessing.Pool(processes=multiprocessing.cpu_count()) as pool:
                    for job in pool.imap_unordered(ghost_worker, todo, chunksize=SAMPLING*2):
                        if job is not None:
                            saver.mandel[job[0]] += job[1]
            else:
                for y in range(0, SIZE * SAMPLING):
                    c_r = ((y / (SIZE * SAMPLING)) * 3.0) - 2.0
                    was_in_period = [False]
                    calc_mandel(None, c_r, c_i, was_in_period=was_in_period)
                    if was_in_period[0]:
                        saver.mandel[x // SAMPLING + (y // SAMPLING) * SIZE] += 1

            skip += 1
            if skip == SAMPLING * ROWS_PER_FRAME:
                skip = 0
                saver.save_frame(None, "Ghost, %8.4f" % (c_i,), None)

    for x in range(SIZE):
        for y in range(SIZE):
            saver.mandel[x + y * SIZE] /= SAMPLING * SAMPLING

    # Keep a height map.  Actually, three of em, one for each of RGB
    hits = []
    for _ in range(SIZE*SIZE):
        hits.append([0,0,0])
    # Use to skip output of frames, once it hits ROWS_PER_FRAME, we draw the frame
    skip = 0
    # Just track if we hit _any_ pixels at all, if we didn't don't bother saving the frame
    pixels_hit = 0
    # And also track the total number of pixels hit overall
    pixels_hit_total = 0
    # And finally, track the shutter size
    pixels_hit_shutter = 0

    # The bits we need for mutliprocessing
    if USE_MULTIPROCESSING:
        queue = multiprocessing.Queue()
        queue_done = multiprocessing.Queue()
        procs = [multiprocessing.Process(target=worker, args=(x, queue, queue_done)) for x in range(multiprocessing.cpu_count())]
        [x.start() for x in procs]
        batch = []

    # Run through each pixel
    last_shutter = None

    if OFF_X == 0:
        min_x = int(0 - ((SIZE * 0.05) * SAMPLING) + 0.5)
        max_x = int((SIZE * SAMPLING) + ((SIZE * 0.05) * SAMPLING) + 0.5)
    else:
        min_x = int((-OFF_X * SAMPLING) - ((SIZE * 0.05) * SAMPLING) + 0.5)
        max_x = int(((SIZE + OFF_X) * SAMPLING) + ((SIZE * 0.05) * SAMPLING) + 0.5)

    min_y = int(0 - ((SIZE * 0.05) * SAMPLING) + 0.5)
    max_y = int((SIZE * SAMPLING) + ((SIZE * 0.05) * SAMPLING) + 0.5)

    # And a note on the ranges we're using.  We're actually rendering 
    # -2 to 1 on the real axis and -1.5 to 1.5 on the imaginary axis.
    # However, we want to draw -2.05 to 2.05 on each axis so we can catch 
    # some edge case artifacts, anything outside of the circle of 2 
    # units around 0,0 won't render anything.  Furthermore, Because we 
    # render 3 units wide, but draw 4.1 units wide, we need to scale up
    # by 4.1/3rds to ensure each tick of c_i and c_r align evenly to a 
    # subdivision of a real pixel.
    for c_i_pixel in range(min_x, max_x + 1):
        c_i = ((c_i_pixel / (SIZE * SAMPLING)) * 3.0) - 1.5

        if last_shutter is None:
            last_shutter = c_i
        pixels_hit = 0

        for c_r_pixel in range(min_y, max_y + 1):
            c_r = ((c_r_pixel / (SIZE * SAMPLING)) * 3.0) - 2.0

            if USE_MULTIPROCESSING:
                batch.append((c_i, c_r))
                if len(batch) >= SAMPLING * SAMPLING:
                    queue.put(('work', batch))
                    batch = []
            else:
                batch = [defaultdict(int), defaultdict(int), defaultdict(int)]
                calc_mandel(batch, c_i, c_r)
                for rgb in range(3):
                    for i, count in batch[rgb].items():
                        hits[i][rgb] += count
                        pixels_hit += count
                        pixels_hit_total += count
                        pixels_hit_shutter += count

        # And dump out the frame if we've done enough work
        skip += 1
        draw_time = False
        if skip == SAMPLING * ROWS_PER_FRAME:
            draw_time = True
        
        if TARGET_PIXS_FRAME is not None:
            if pixels_hit_shutter >= TARGET_PIXS_FRAME:
                draw_time = True
        
        if draw_time:
            if USE_MULTIPROCESSING:
                if len(batch) > 0:
                    queue.put(('work', batch))
                    batch = []
                [queue.put(('flush',)) for _ in procs]
                for _ in procs:
                    temp = queue_done.get()
                    for rgb in range(3):
                        for i, count in temp[rgb]:
                            hits[i][rgb] += count
                            pixels_hit += count
                            pixels_hit_total += count
                
            skip = 0
            pixels_hit_shutter = 0
            # Figure out where the "shutter" should be
            shutter = [
                int(((last_shutter + 1.5) / 3) * (SIZE) + 100000.5) - 100000,
                int(((c_i + 1.5) / 3) * (SIZE) + 100000.5) - 100000,
            ]
            last_shutter = None
            saver.save_frame(hits, "%7.4f x %7.4f" % (0, c_i), shutter, draw=shutter[1]>= -OFF_X and shutter[0]<SIZE+OFF_X*2)


    if USE_MULTIPROCESSING:
        if len(batch) > 0:
            queue.put(('work', batch))
            batch = []
        [queue.put(('flush',)) for _ in procs]
        for _ in procs:
            temp = queue_done.get()
            for rgb in range(3):
                for i, count in temp[rgb]:
                    hits[i][rgb] += count
                    pixels_hit += count
        [queue.put(('close',)) for _ in procs]
        [x.join() for x in procs]

    # And force the final frame, we want to save it even if nothing's
    # different from the last one since there won't be a shutter, 
    # also repeat it a few times to give us something to "hit" in
    # case the video loops
    saver.save_frame(hits, "final", None, draw=True, repeat=30)

    # Show the total number of pixels hit, this can be used to control
    # the speed of the shutter in an optional mode
    print("Total pixels hit: " + str(pixels_hit_total))


if __name__ == "__main__":
    main()
