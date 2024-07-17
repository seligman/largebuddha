#!/usr/bin/env python3

from datetime import datetime
from PIL import Image
import json
import math
import numpy as np
import os
import pickle
import sys
import time

SETTINGS = {
    "res": [1024, 1024],
    "off": [0, 0],
    "skip": 1,
    "device": "cuda",
    "iters": [15, 500_000],
    "cycles": 1_000_000_000, # About 1 minute
    # "cycles": 10_000_000_000, # About 10 minutes
    # "cycles": 100_000_000_000, # About 105 minutes
}

def get_lock():
    # This helper prevents multiple runners on a desktop PC, it
    # no-ops if this directory is not present
    if os.path.isdir(r"C:\Code\Projects\Docker\WhisperX"):
        old_cwd = os.getcwd()
        os.chdir(r"C:\Code\Projects\Docker\WhisperX")
        sys.path.append(".")
        from locker import Locker # type: ignore
        sys.path.pop(-1)
        os.chdir(old_cwd)
        return Locker()
    else:
        class Dummy:
            def close(self):
                pass
        return Dummy()

def get_calc():
    # Main kernel for CUDA work
    import warp as wp # type: ignore

    # Pull out settings so the transpiler doesn't need to do dictionary lookups
    width, height = SETTINGS["res"]
    min_iters, max_iters = SETTINGS["iters"]
    skip = SETTINGS["skip"]
    skip_off_x, skip_off_y = SETTINGS["off"]

    @wp.kernel
    def calc(
        # heights: wp.array(dtype=wp.int32, shape=(width, height)), # type: ignore
        reals: wp.array(dtype=wp.float32, shape=(width, height)), # type: ignore
        imags: wp.array(dtype=wp.float32, shape=(width, height)), # type: ignore
        seed: wp.int32,
        ):

        # Create a random number, seeded off of the current thread ID
        state = wp.rand_init(seed, wp.tid())

        # Pick a start value, just a random value from -2 -> 2
        x, y = wp.randf(state), wp.randf(state)
        c_x = (x * 4.0) - 2.0
        c_y = (y * 4.0) - 2.0

        # This is the offset of the view area
        off_x, off_y = 2.0, 1.5

        iters = int(0)
        x, y = float(0), float(0)

        # See if this value is inside one of the two main periods, if so
        # just treat the value as being at max_iters, which short-circuits calcs
        qt1 = c_x - 0.25
        qt2 = c_y * c_y
        q = qt1 * qt1 + qt2
        q *= q + qt1
        if q < qt2 * 0.25:
            iters = max_iters
        else:
            q = c_x + 1.0
            q = q * q + qt2
            if q < 0.0625:
                iters = max_iters

        # See if this value escapes the set or not
        while x * x + y * y <= 4 and iters < max_iters:
            x, y = (x * x - y * y + c_x), ((2.0 * x * y) + c_y)
            iters += 1

        if iters < max_iters:
            # The initial value does escape, so go ahead and calculate it again
            # and store the results in the global memory
            final_x, final_y = x, y
            # Normalize the final point to be one unit away from the center
            dist = wp.sqrt(final_x * final_x + final_y * final_y)
            final_x /= dist
            final_y /= dist

            x, y = float(0), float(0)
            iters = int(0)
            while x * x + y * y <= 4.0:
                x, y = (x * x - y * y + c_x), ((2.0 * x * y) + c_y)
                iters += 1
                if iters > min_iters:
                    # Only store values after some min number of iterations
                    x_i = wp.int32((1.0 / 6.0) * (2.0 * x + (off_x * 2.0)) * float(width * skip))
                    y_i = wp.int32((1.0 / 6.0) * (2.0 * y + (off_y * 2.0)) * float(height * skip))
                    if x_i % skip == skip_off_x and y_i % skip == skip_off_y:
                        # This is a value in the target area to store
                        x_i //= skip
                        y_i //= skip
                        if 0 <= x_i < width and 0 <= y_i < height:
                            # And this value is inside of the final preview, go ahead and
                            # atomiclly add it to prevent collisions with other threads
                            # wp.atomic_add(heights, x_i, y_i, 1)
                            wp.atomic_add(reals, x_i, y_i, final_x)
                            wp.atomic_add(imags, x_i, y_i, final_y)
    return wp, calc

class Timer:
    # Simple helper to log output messages with a timestamp
    def __init__(self):
        self.start = time.time()
    
    def __call__(self, val):
        end = time.time()
        dur = end - self.start
        if dur < 5: dur = f"{int(dur * 1000):d}ms"
        elif dur < 90: dur = f"{dur:.1f}s"
        elif dur < 3600: dur = f"{dur/60:.1f}m"
        else: dur = f"{dur/3600:.1f}h"
        print(f"{datetime.now().strftime('%d %H:%M:%S')}: {dur}: {val}")

def main():
    timer = Timer()

    temp = None
    if os.path.isfile("cached.dat"):
        # Derserialze data
        timer("Deserialize...")
        with open("cached.dat", "rb") as f:
            cache_settings, cache_data = pickle.load(f)
        
        if json.dumps(cache_settings, sort_keys=True) != json.dumps(SETTINGS, sort_keys=True):
            # The settings look different, this will trigger a full re-run
            timer("Serialized data is for a different settings")
        else:
            temp = cache_data
    
    if temp is None:
        # Perform the work on CUDA
        lock = get_lock()
        timer("Initialize")
        # Get the warp namespace, and the kernel to do the work
        wp, calc = get_calc()

        # Allocate the memory in the CUDA space
        # heights = wp.zeros((SETTINGS["res"][0], SETTINGS["res"][1]), dtype=wp.int32, device=SETTINGS["device"])
        reals = wp.zeros((SETTINGS["res"][0], SETTINGS["res"][1]), dtype=wp.float32, device=SETTINGS["device"])
        imags = wp.zeros((SETTINGS["res"][0], SETTINGS["res"][1]), dtype=wp.float32, device=SETTINGS["device"])

        # Start the workers, note that they're free running, they'll keep running till we try to access the memory
        wp.launch(kernel=calc, inputs=(
            # heights, 
            reals, 
            imags, 
            wp.int32(42),
        ), dim=SETTINGS["cycles"], device=SETTINGS["device"])

        # Wait for, and copy the memory to our local space (and flatten it to make further work easier)
        timer("Working...")
        # heights = heights.numpy().flatten()
        reals = reals.numpy().flatten()
        imags = imags.numpy().flatten()

        # And re-arrange the memory
        temp = np.vstack([
            # heights, 
            reals, 
            imags,
        ]).T

        # Serialize everything so if we want to make minor output changes, it's much faster
        with open("cached.dat", "wb") as f:
            pickle.dump([SETTINGS, temp], f)
        timer("Done with calc")
        lock.close()

    limit_top, limit_bright, limit_base = 0, 0, 0

    # Helpers to calculate the color for a given pixel
    def arg(r, i):
        div = r*r + i*i
        if div == 0:
            r, i = 0, 0
        else:
            div = math.sqrt(div)
            r, i = (r*div)/(div*div), (i*div)/(div*div)
        return math.atan2(i, r)

    def hsv_to_rgb_pastel(h, S, V):
        perc = h / 360.0

        r = ((math.sin(perc * math.pi * 2 + math.pi / 2) + 1) / 2) * 104 + 69
        g = ((math.sin((perc + 0.5) * math.pi * 2 + math.pi / 2) + 1) / 2) * 41 + 113
        b = ((math.sin((perc + 0.3) * math.pi * 2 + math.pi / 2) + 1) / 2) * 99 + 71

        r = ((r / 255.0) * S + (1 - S)) * V
        g = ((g / 255.0) * S + (1 - S)) * V
        b = ((b / 255.0) * S + (1 - S)) * V

        return r, g, b

    def get_abs(val):
        real, img = val
        abs = math.sqrt(real*real + img*img)
        hue = (arg(real, img) / math.pi) * 360.0
        return hue, abs

    def get_color(val):
        hue, abs = val

        lum = abs / limit_top
        lum = lum * 0.95 + 0.05
        lum = min(lum, 1.0)
        if abs <= limit_base:
            lum = 0
        sat = 1.0
        if abs >= limit_top:
            if abs >= limit_bright:
                sat = 0
            else:
                sat = 1.0 - ((abs - limit_top) / (limit_bright - limit_top))

        lower = True
        if lum > 0.2:
            lower = False
        
        if lower:
            lum /= 0.2
        else:
            lum = (lum - 0.2) / (1.0 - 0.2)
        
        lum = math.pow(lum, 0.85)

        if lower:
            lum *= 0.2
        else:
            lum = (lum * (1.0 - 0.2)) + 0.2
        
        r, g, b = hsv_to_rgb_pastel(hue, sat, lum)
        return r * 255, g * 255, b * 255

    in_order = None
    def find_limit(desc, target):
        nonlocal in_order
        if in_order is None:
            in_order = np.sort(np.array(list(x[1] for x in temp)))
        target = in_order[int(len(in_order) * target)]
        print(f"{desc}: {target}")
        return target

    # Get the core values first, so we can find the limits
    timer("Get abs...")
    temp = np.array(list(map(get_abs, temp)))

    # Find the limits for the colors
    # TODO: These values should be cached between off_x, off_y runs so they don't change from
    # run to run
    timer("Calc limits...")
    limit_base = find_limit("limit_base", 0.25)
    limit_top = find_limit("limit_top", 0.995)
    limit_bright = find_limit("limit_bright", 0.9999)

    # This uses the limits and some color math to get the RGB color for each pixel
    timer("Get colors...")
    temp = np.array(list(map(get_color, temp)))

    # Turn the 1D array into a 2D array for PIL's use
    timer("Reshape...")
    temp = temp.reshape((SETTINGS["res"][0], SETTINGS["res"][1], 3))

    # And create a final PNG file
    timer("Create image...")
    im = Image.fromarray(temp.astype(np.uint8))
    timer("Save image...")
    im.save(f"output_{SETTINGS['off'][0]:03d}_{SETTINGS['off'][1]:03d}.png")

    # And we're done!
    timer("All done.")

if __name__ == "__main__":
    main()
