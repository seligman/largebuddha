#!/usr/bin/env python3

from options import OPTIONS, show_flags
import os
if OPTIONS["show_gui"]:
    # Only try loading pygame if needed
    os.environ['PYGAME_HIDE_SUPPORT_PROMPT'] = "hide"
    import pygame
if OPTIONS["multiproc"]:
    # Only need to bring in multiprocessing if needed
    import multiprocessing
from collections import deque, defaultdict
from datetime import datetime
from PIL import Image
import numpy as np
import heapq
import json
import math
import subprocess
import time
import mandelbrot_native_helper

def gui_to_mand(pt_x, pt_y, xo=0, yo=0, alias=1, center_x=OPTIONS["mand_loc"]["x"], center_y=OPTIONS["mand_loc"]["y"], size=OPTIONS["mand_loc"]["size"]):
    # Helper to turn a point on screen to an absolute point inside the mandelbrot
    x = (((pt_x + max(0, (OPTIONS['height'] - OPTIONS['width']) / 2)) + xo / alias) / max(OPTIONS['width'], OPTIONS['height'])) * size - ((size / 2) + center_x)
    y = (((pt_y + max(0, (OPTIONS['width'] - OPTIONS['height']) / 2)) + yo / alias) / max(OPTIONS['width'], OPTIONS['height'])) * size - ((size / 2) + center_y)
    return x, y

def mand_to_gui(x, y):
    # Helper to turn a point along the Mandelbrot to a GUI image, opposite of gui_to_mand
    center_x, center_y, size = OPTIONS["mand_loc"]["x"], OPTIONS["mand_loc"]["y"], OPTIONS["mand_loc"]["size"]
    xo, yo, alias = 0, 0, 1
    result_x = ((max(OPTIONS['width'], OPTIONS['height']) * (2 * center_x + size + 2 * (x + xo / alias))) / (2 * size)) - (max(0, (OPTIONS['height'] - OPTIONS['width']) / 2))
    result_y = ((max(OPTIONS['width'], OPTIONS['height']) * (2 * center_y + size + 2 * (y + yo / alias))) / (2 * size)) - (max(0, (OPTIONS['width'] - OPTIONS['height']) / 2))
    return result_x, result_y

def draw_mand(alias, size, center_x, center_y, julia=None, max_iters=50, max_skip=0):
    # State machine to draw a Mandelbrot or Julia
    # All of these state machines return via yield from time to time to update the UI, and otherwise
    # pass results back to the main caller.  They'll end by returning None before a StopIteration
    # is raised.

    # The main engine to call, in this function, use none of it's cache options
    worker = MandelEngine(max_iters=max_iters, cache='none')

    done = set()
    yield {"type": "msg", "msg": f"Drawing fractal..."}
    # Draw at different levels, so we can "resolve" the view of the image over time
    for skip in [32, 16, 8, 4, 2, 1]:
        if skip < max_skip:
            break
        yield {"type": "msg", "msg": f"Working on pass {skip}...", "info": True}

        for pt_y in range(0, OPTIONS['height'], skip):
            for pt_x in range(0, OPTIONS['width'], skip):
                if (pt_x, pt_y) not in done:
                    done.add((pt_x, pt_y))

                    all_rgb = [0, 0, 0]
                    final_escape = None
                    final_smoothed = None

                    # Calculate several points within this pixel for alias
                    for xo in range(alias):
                        for yo in range(alias):
                            x, y = gui_to_mand(pt_x, pt_y, xo, yo, alias, center_x, center_y, size)

                            if worker.calc_mand(x, y, julia=julia):
                                # Inside the set, so use a default color
                                rgb = (50, 0, 0)
                            else:
                                # Smooth out the iteration
                                log_zn = math.log(worker.final_dist) / 2
                                nu = math.log(log_zn / math.log(2)) / math.log(2)
                                if xo == 0 and yo == 0:
                                    final_escape = worker.escaped_at
                                    final_smoothed = worker.escaped_at + 1 - nu

                                # Pick a color along a simple palette
                                rgb = ((worker.escaped_at + 1 - nu) / max_iters)
                                rgb = max(0, min(1, rgb)) * 4
                                colors = [
                                    (0, 0, 100),
                                    (255, 255, 255),
                                    (255, 180, 0),
                                    (100, 0, 0),
                                    (100, 0, 0),
                                ]
                                
                                rgb = (
                                    int(colors[int(rgb)][0] * (1 - (rgb - int(rgb))) + colors[int(rgb) + 1][0] * (rgb - int(rgb))),
                                    int(colors[int(rgb)][1] * (1 - (rgb - int(rgb))) + colors[int(rgb) + 1][1] * (rgb - int(rgb))),
                                    int(colors[int(rgb)][2] * (1 - (rgb - int(rgb))) + colors[int(rgb) + 1][2] * (rgb - int(rgb))),
                                )
                            
                            all_rgb[0] += rgb[0]
                            all_rgb[1] += rgb[1]
                            all_rgb[2] += rgb[2]

                    # Smooth out the multiple colors
                    rgb = (int(all_rgb[0] / (alias * alias)), int(all_rgb[1] / (alias * alias)), int(all_rgb[2] / (alias * alias)))

                    # Send this point into the caller
                    yield {
                        "type": "draw_mand", 
                        "x": pt_x, "y": pt_y, 
                        "rgb": rgb, 
                        "skip": skip, 
                        "escape": final_escape, 
                        "smoothed": final_smoothed,
                    }

    yield {"type": "msg", "msg": "Done Drawing"}
    yield None

def fill_pool(state):
    # State machine to show the "pool", mostly just used for some simple debugging
    yield {"type": "msg", "msg": "Finding Pool"}
    for y in range(OPTIONS['height']):
        for x in range(OPTIONS['width']):
            if (x, y) in state.pool:
                rgb = state.pool[(x, y)]
                rgb = [
                    int((rgb[0] + 128) / 2),
                    int((rgb[1] + 0) / 2),
                    int((rgb[2] + 0) / 2),
                ]
                yield {
                    "type": "draw_pool",
                    "x": x,
                    "y": y,
                    "rgb": rgb,
                }

    yield {"type": "msg", "msg": "Done With Pool"}
    yield None

class MandelEngine:
    # A class that can answer the question for a high-resolution fractal:  
    #       Is this point inside a Mandelbrot set?
    # It uses two bit-arrays as a cache to keep memory pressure down and never
    # have to calculate the same pixel twice
    def __init__(self, max_iters, cache):
        self.pixel = OPTIONS["scan_size"]
        self.max_iters = max_iters
        self.escaped_at = None
        self.final_dist = None
        self.cache_bit = False
        self.cache_set = False
        if cache == 'bits':
            import bitarray
            # The in/out are 1/2 size since they're reflected on the x axis, with an extra row for zero
            self.mand_in = bitarray.bitarray((self.pixel * 2 + 1) * (self.pixel * 4))
            self.mand_out = bitarray.bitarray((self.pixel * 2 + 1) * (self.pixel * 4))
            # The seen is full size
            self.seen = bitarray.bitarray((self.pixel * 4) * (self.pixel * 4))
            self.mand_in.setall(0)
            self.mand_out.setall(0)
            self.seen.setall(0)
            self.cache_bit = True
        elif cache == 'set':
            self.mand = {}
            self.mand_buffer = deque()
            self.seen_set = set()
            self.seen_buffer = deque()
            self.cache_set = True
        elif cache == 'none':
            pass
        else:
            raise Exception('Unknown cache mode')

    def calc_mand(self, x, y, julia=None):
        if julia is None:
            in_set, self.escaped_at, self.final_dist = mandelbrot_native_helper.calc(x, y, False, 0.0, 0.0, self.max_iters)
        else:
            in_set, self.escaped_at, self.final_dist = mandelbrot_native_helper.calc(x, y, True, julia[0], julia[1], self.max_iters)
        return in_set == 1

    def calc_mand_python(self, x, y, julia=None):
        # Calculate one point, doesn't use cache, points should be in natural coords
        # Returns True if the point is in the set, False otherwise.  On False
        # self.escaped_at is the escaped iteration, and self.final_dist is the final
        # escape distance.  Does not cache any result

        self.escaped_at = None
        self.final_dist = None

        if julia is None:
            p = math.sqrt(((x - 1/4) ** 2) + (y*y))
            if x <= p - (2 * (p ** 2)) + (1/4):
                # This point is in the main cardioid
                return True
            elif (x + 1) ** 2 + (y * y) <= 1/16:
                # This point is in the first circular bulb
                return True

        u, v = (x, y) if julia is None else julia
        cycle = [None] * 500
        for i in range(self.max_iters):
            x, y = x * x - y * y + u, 2 * x * y + v
            dist = x * x + y * y

            if dist >= 25:
                self.escaped_at = i
                self.final_dist = dist
                return False

        return True

    def _get_bit(self, bits, x, y):
        if self.pixel * -3 <= x <= self.pixel * 1:
            if self.pixel * -2 <= y < self.pixel * 2:
                return bits[(x + (self.pixel * 3)) + ((y + (self.pixel * 2)) * (self.pixel * 4))] == 1
        # Out of bound, do nothing
        return False

    def _set_bits(self, bits, x, y, value):
        if self.pixel * -3 <= x <= self.pixel * 1:
            if self.pixel * -2 <= y < self.pixel * 2:
                # Find the bit to set
                index, offset = divmod((x + (self.pixel * 3)) + ((y + (self.pixel * 2)) * (self.pixel * 4)), 32)
                if value:
                    bits[(x + (self.pixel * 3)) + ((y + (self.pixel * 2)) * (self.pixel * 4))] = 1
                else:
                    bits[(x + (self.pixel * 3)) + ((y + (self.pixel * 2)) * (self.pixel * 4))] = 0

    def is_in_set(self, x, y):
        # Return True if a point is in the set, False otherwise, expects integer location scaled
        # by self.pixel units
        # First off, see if it's in either set:

        # Reflect any positive Y value to speed up lookups by forcing cache hits when we mirror things
        y = -abs(y)

        if self.cache_bit:
            if self.get_bit(self.mand_in, x, y):
                return True
            if self.get_bit(self.mand_out, x, y):
                return False
        elif self.cache_set:
            ret = self.mand.get((x, y), None)
            if ret is not None:
                return ret

        # Not cached already, go ahead and calculate the bit:
        ret = self.calc_mand(x / self.pixel, y / self.pixel)

        # Store the results
        if self.cache_bit:
            if ret:
                self.set_bits(self.mand_in, x, y, True)
            else:
                self.set_bits(self.mand_out, x, y, True)
        elif self.cache_set:
            self.mand[(x, y)] = ret
            self.mand_buffer.append((x, y))
            if len(self.mand_buffer) > 1000:
                del self.mand[self.mand_buffer.popleft()]
        
        return ret
    
    def has_been_seen(self, x, y):
        if self.cache_bit:
            ret = self._get_bit(self.seen, x, y)
            if ret == 0:
                self._set_bits(self.seen, x, y, 1)
            return ret == 1
        elif self.cache_set:
            if (x, y) in self.seen_set:
                return True
            else:
                self.seen_set.add((x, y))
                self.seen_buffer.append((x, y))
                if len(self.seen_buffer) > 1000000:
                    self.seen_set.remove(self.seen_buffer.popleft())
                return False
        else:
            raise Exception("No cache")

    def is_border(self, x, y):
        # Return True if this point is a "border", in other words, is not in the set, but touches
        # at least on pixel that is in the set.  Uses self.pixel integer units
        if self.is_in_set(x, y):
            return False
        
        if self.is_in_set(x - 1, y - 1): return True
        if self.is_in_set(x, y - 1): return True
        if self.is_in_set(x + 1, y - 1): return True
        if self.is_in_set(x + 1, y): return True
        if self.is_in_set(x + 1, y + 1): return True
        if self.is_in_set(x, y + 1): return True
        if self.is_in_set(x - 1, y + 1): return True
        if self.is_in_set(x - 1, y): return True
        
        return False

def find_edge():
    # State machine to find the border of the mandelbrot, does so by a simple A* scan around the border
    yield {"type": "msg", "msg": "Filling Edge"}
    pixel = OPTIONS["scan_size"]

    show_msg("Starting scan for border")
    bits = MandelEngine(max_iters=OPTIONS["border_iter"], cache='set')

    # Start scanning in the center of the Mandelbrot
    x, y = 0, 0
    while not bits.is_border(x + 1, y):
        x += 1
    while bits.is_border(x + 1, y):
        x += 1

    # Make sure the target final point is actually a border unit
    tx, ty = x, y - 1
    if not bits.is_border(tx, ty):
        raise Exception("The start and end don't connect!")

    # An A* algo to find the border along the mand
    final_trail = deque()
    final_head = None
    # The point where the A* algo is allowed to start searching up
    unleash = False

    # A priority queue to keep searching the "cheapest route"
    todo = []
    heapq.heappush(todo, (0, x, y, None))

    # Dump out some message every now and then for the GUI mode
    at = time.time() + 0.5
    attempts = 0
    cost_check = 0

    while True:
        attempts += 1
        cost, x, y, history = heapq.heappop(todo)
        if (cost >= cost_check and history is not None and len(todo) == 0) or (x, y) == (tx, ty):
            # For the last item, as well as every now and then, add the current trail we have
            # to shrink down memory usage.  This is also where we drop points along the edge
            # so we don't end up rendering millions of items.
            cost_check += 5000
            temp = deque()
            # Invert the queue, and pop out the item we care about
            while history is not None:
                temp.append((history[1], history[2]))
                history = history[3]
            # Place the inverted queue on our final queue
            while len(temp):
                cur = temp.pop()
                if final_head is None or math.sqrt(((final_head[0] - cur[0]) ** 2) + ((final_head[1] - cur[1]) ** 2)) / pixel >= OPTIONS["frame_spacing"]:
                    final_trail.append(cur)
                    final_head = cur
            history = None
            if (x, y) == (tx, ty):
                # We hit the end point, so we're all done!
                break

        if OPTIONS["show_gui"]:
            if time.time() >= at:
                yield {"type": "msg", "msg": f"Border, working, at {cost:,}, {attempts:,} attempts, {len(todo):,} queue size, {len(final_trail):,} total frames"}
                yield {"type": "show_loc", "status": "show", "x": x / pixel, "y": y / pixel}
                at = time.time() + 0.5
        else:
            if time.time() >= at:
                show_msg(f"Border: Cost: {cost:,}, Frames: {len(final_trail):,}, Loc: {x/pixel:0.4f} x {y/pixel:0.4f}")
                at = time.time() + 60

        # Check all the touching points of this
        for ox, oy in [[x - 1, y - 1], [x + 1, y - 1], [x - 1, y + 1], [x + 1, y + 1], [x + 1, y], [x - 1, y], [x, y + 1], [x, y - 1]]:
            skip = False
            if not unleash:
                # For the start, only go down
                if oy < 0:
                    skip = True
                if ox <= -1.95 * pixel:
                    unleash = True

            if not skip:
                # If we've seen this point, don't bother with it again
                if bits.has_been_seen(ox, oy):
                    skip = True

            if not skip:
                if bits.is_border(ox, oy):
                    # Ok, this point is possibly part of a path, go ahead and add it to our queue
                    heapq.heappush(todo, (cost + 1, ox, oy, (cost, x, y, history)))

    if OPTIONS["show_gui"]:
        yield {"type": "show_loc", "status": "hide"}
    if final_trail is None:
        raise Exception("Unable to find path to connect the start and end!")

    # Now get the list of all points in our little data objects to build a simple list
    final_trail = [x for x in final_trail]

    # dump = []
    # with open("dump.jsonl", "rt") as f:
    #     for row in f:
    #         dump.append(tuple(json.loads(row)))
    # errors = 0
    # if len(dump) != len(final_trail):
    #     print(f"Dump of {len(dump)} != final trail of {len(final_trail)}")
    #     errors += 1
    # for i, (ft_val, d_val) in enumerate(zip(final_trail, dump)):
    #     if ft_val != d_val:
    #         print(f"At {i}, val of {ft_val} != {d_val}")
    #         errors += 1
    #         if errors >= 15:
    #             break
    # if errors == 0:
    #     print("All good!")
    # with open("dump.jsonl", "wt") as f:
    #     for cur in final_trail:
    #         f.write(json.dumps(cur) + "\n")
    # exit(0)

    # Append the first frame to the end so we start where we ended
    final_trail.append(final_trail[0])
    show_msg(f"Found trail of {len(final_trail):,} items")

    # Animate the trail that we found, just to give some idea if it did the right thing
    skip = max(1, len(final_trail) // 250)
    for i, (x, y) in enumerate(final_trail):
        yield {
            "type": "draw_edge",
            "x": x / pixel,
            "y": y / pixel,
            "rgb": (255, 255, 255),
            "first": i == 0,
            "last": i == (len(final_trail) - 1),
        }
        if i % skip == 0:
            yield {"type": "animate"}

    yield {"type": "msg", "msg": "Done With Edge"}
    yield None

def save_frame(fn):
    # State machine to trigger the save of a frame
    yield {"type": "save_frame", "fn": fn}
    yield None

def dupe_frame(source, dest):
    # State machine to trigger the duplication of an existing result
    yield {"type": "dupe_frame", "source": source, "dest": dest}
    yield None

def set_target(x, y):
    # State machine to note a specific point of which point we're using for the Julia
    yield {"type": "set_target", "x": x, "y": y}
    yield None

def show_msg(value):
    # Simple helper to show a message with a timestamp
    print(datetime.utcnow().strftime("%d %H:%M:%S: ") + value)

def multiproc_worker(row):
    # This is the main worker for a process being called from multiproc mode
    # Just a slimmed down version of main() that does no UI, and only works
    # on one frame then exits

    if os.path.isfile("abort.txt"):
        return None

    engines = []
    state = State()
    add_frame(engines, row)
    for engine in engines:
        while True:
            job = next(engine)
            proc = None
            if job is None:
                break
            elif job['type'] == 'draw_mand':
                proc = handle_draw_mand
            elif job['type'] == 'save_frame':
                proc = handle_save_frame
            elif job['type'] == 'dupe_frame':
                proc = handle_dupe_frame
            elif job['type'] == 'set_target':
                proc = handle_set_target
            
            if proc is not None:
                proc(state, job, show_msg=lambda x: None)

    # Just return something so the caller knows what we did
    return row['dest']

def main_multiproc():
    # Simplified version of main() that launches multiple workers on different cores
    if os.path.isfile("abort.txt"):
        os.unlink("abort.txt")

    show_msg("Working...")
    jobs, dupes = [], defaultdict(list)
    with open("frames.jsonl") as f:
        # Pull in work units
        for row in f:
            row = json.loads(row)
            if not os.path.isfile(row["dest"]):
                if "requires" in row:
                    dupes[row["requires"]].append(row)
                else:
                    jobs.append(row)
        
    # Start the workers
    args = {}
    if "procs" in OPTIONS:
        args["processes"] = OPTIONS["procs"]
    with multiprocessing.Pool(**args) as pool:
        for fn in pool.imap_unordered(multiproc_worker, jobs):
            if fn is not None:
                show_msg(f"Wrote {fn}")
                if OPTIONS["multiproc_sync"]:
                    subprocess.check_call(["python3", "sync.py", "single", fn])
                # Make sure to run any processes that depend on this one
                engines = []
                for row in dupes[fn]:
                    add_frame(engines, row)
                for engine in engines:
                    while True:
                        job = next(engine)
                        if job is None:
                            break
                        elif job['type'] == 'dupe_frame':
                            handle_dupe_frame(State(), job)
                            if OPTIONS["multiproc_sync"]:
                                subprocess.check_call(["python3", "sync.py", "single", job["dest"]])
    
    show_msg("Done")

def add_frame(engines, row):
    # Helper to create the state machine workers for a given frame
    if isinstance(row, str):
        row = json.loads(row)
    if not os.path.isfile(row["dest"]):
        if row["cmd"] == "draw":
            engines.append(set_target(**row["set"]))
            engines.append(draw_mand(**row["mand"]))
            engines.append(save_frame(row["dest"]))
        elif row["cmd"] == "dupe":
            engines.append(dupe_frame(row["source"], row["dest"]))
        else:
            raise Exception(f"Unknown command: {row}")

def handle_draw_mand(state, job, show_msg=show_msg):
    # Handle a draw event from a state machine
    if state.target is None:
        # When drawing the mandelbrot, save it so we 
        # can quickly draw it later on top of the Julia set
        alpha = job['smoothed']
        if alpha is None:
            alpha = 1
        else:
            a, b = 1, 4
            alpha = max(a, min(alpha, b))
            alpha = (alpha - a) / (b - a)
        if alpha > 0:
            state.preview.append((alpha, list(job['rgb']), job['x'], job['y']))

    if job['escape'] is None or job['escape'] >= 10 and 'julia' not in job:
        # Track the pool, so we can highlight it later, useful to see where
        # things are happening
        state.pool[(job['x'], job['y'])] = job['rgb']

    # And light up the pixels we were told about
    for xo in range(job['skip']):
        for yo in range(job['skip']):
            if job['x'] + xo < OPTIONS["width"] and job['y'] + yo < OPTIONS["height"]:
                state.pixels[job['y'] + yo, job['x'] + xo] = job['rgb']
                if OPTIONS["show_gui"]:
                    state.screen.set_at(((job['x'] + xo) // OPTIONS["gui_shrink"], (job['y'] + yo) // OPTIONS["gui_shrink"]), job['rgb'])

def handle_dupe_frame(state, job, show_msg=show_msg):
    # Handle a dupe frame event, just copy the image
    show_msg(f"Dupe {job['source']} to {job['dest']}")
    with open(job['source'], 'rb') as f_source:
        with open(job['dest'], 'wb') as f_dest:
            f_dest.write(f_source.read())

def handle_save_frame(state, job, show_msg=show_msg):
    # Handle a save frame event
    
    # If the preview file exists, load the data so we can add the Mandelbrot image on top
    if os.path.isfile("frame_preview.jsonl"):
        with open("frame_preview.jsonl", "rb") as f:
            for row in f:
                alpha, rgb, x, y = json.loads(row)
                rgb[0] = int(rgb[0] * alpha + state.pixels[y, x, 0] * (1 - alpha))
                rgb[1] = int(rgb[1] * alpha + state.pixels[y, x, 1] * (1 - alpha))
                rgb[2] = int(rgb[2] * alpha + state.pixels[y, x, 2] * (1 - alpha))
                if OPTIONS["show_gui"]:
                    state.screen.set_at((x // OPTIONS["gui_shrink"], y // OPTIONS["gui_shrink"]), rgb)
                state.pixels[y, x] = rgb

    # Draw the cross hairs and circle where the Julia is pulling its point from
    size_outer = int(OPTIONS['width'] / 125)
    size_inner = size_outer * 0.85
    size_hairs = size_outer * 0.05
    pt_x, pt_y = mand_to_gui(state.target['x'], state.target['y'])
    pt_x, pt_y = int(pt_x), int(pt_y)
    for x in range(-(size_outer+5), size_outer+5):
        for y in range(-(size_outer+5), size_outer+5):
            rgb, alpha = 0, 0
            for xo in range(-2, 3):
                for yo in range(-2, 3):
                    dist = math.sqrt(((x + (xo / 5)) ** 2) + ((y + (yo / 5)) ** 2))
                    if dist <= size_inner:
                        if abs(x + (xo / 5)) <= size_hairs or abs(y + (yo / 5)) <= size_hairs:
                            rgb, alpha = rgb + 255, alpha + 1.0
                        else:
                            rgb, alpha = rgb + 255, alpha + 0.5
                    elif dist <= size_outer:
                        rgb, alpha = rgb + 60, alpha + 1
            if alpha > 0:
                alpha = alpha / 25
                rgb = rgb / 25
                rgb = [
                    int((rgb * alpha) + (state.pixels[pt_y + y, pt_x + x, 0] * (1 - alpha))),
                    int((rgb * alpha) + (state.pixels[pt_y + y, pt_x + x, 1] * (1 - alpha))),
                    int((rgb * alpha) + (state.pixels[pt_y + y, pt_x + x, 2] * (1 - alpha))),
                ]
                if OPTIONS["show_gui"]:
                    state.screen.set_at(((pt_x + x) // OPTIONS["gui_shrink"], (pt_y + y) // OPTIONS["gui_shrink"]), rgb)
                state.pixels[pt_y + y, pt_x + x] = rgb

    # All done, save out the PNG file
    if OPTIONS["save_results"]:
        im = Image.new('RGB', (OPTIONS['width'], OPTIONS['height']), (0, 0, 0))
        for x in range(OPTIONS['width']):
            for y in range(OPTIONS['height']):
                im.putpixel((x, y), tuple(state.pixels[y, x]))
        im.save(job['fn'])
        im.close()
    show_msg(f"Saved {job['fn']}")

def handle_set_target(state, job, show_msg=show_msg):
    # Helper to note the position of the Julia set
    state.target = job

class State:
    # State information for our main worker, useful to pass the current GUI information, along
    # with the information necessary to save the image
    def __init__(self):
        self.pool = {}
        self.preview = []
        self.pixels = np.zeros(
            (
                OPTIONS['height'], 
                OPTIONS['width'], 
                3
            ), 
            dtype=np.uint8,
        )
        self.target = None
        self.screen = None

def main():
    # The main worker, mix GUI stuff, but try to keep anything necessary to actually render fractals
    # elsewhere

    state = State()
    if OPTIONS["show_gui"]:
        # Only setup pygame stuff if the GUI is requested
        state.screen = pygame.display.set_mode((OPTIONS['width'] // OPTIONS["gui_shrink"], OPTIONS['height'] // OPTIONS["gui_shrink"]))
        pygame.display.set_caption('Mandelbrot')
        pygame.display.flip()
        pygame.display.update()

    # Kee a list of state machines to work on
    engines = []
    def append_mand():
        engines.append(draw_mand(
            alias=1 if OPTIONS["quick_mode"] else 2, 
            size=OPTIONS["mand_loc"]["size"], 
            center_x=OPTIONS["mand_loc"]["x"], 
            center_y=OPTIONS["mand_loc"]["y"], 
            max_iters=OPTIONS["mand_iters"],
        ))

    # If we have history from a previous run, pull it in, otherwise 
    # create some starter state machines, they will add others unless
    # we're in View only mode
    if os.path.isfile("frames.jsonl") and not OPTIONS["view_only"]:
        with open("frames.jsonl") as f:
            for row in f:
                add_frame(engines, row)
    else:
        append_mand()
        if not OPTIONS["view_only"]:
            engines.append(fill_pool(state))
            engines.append(find_edge())

    running = True
    show_border = 0
    frame_number = 0
    pointer = {}

    while running:
        if OPTIONS["show_gui"]:
            # Handle the GUI events
            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    running = False
                elif event.type == pygame.MOUSEBUTTONUP:
                    # Just log the real point that was clicked
                    x, y = event.pos
                    x, y = gui_to_mand(x, y)
                    show_msg(f"Click at {x:0.5f} x {y:0.5f}")
                elif event.type == pygame.KEYUP:
                    mand_changed = False
                    # Escape = Quit
                    # Up/Down/Left/Right = Move the Mandelbrot
                    # W/S = Zoom in/out
                    if event.key == pygame.K_ESCAPE:
                        running = False
                    elif event.key == pygame.K_UP:
                        OPTIONS["mand_loc"]["y"] -= 0.25
                        mand_changed = True
                    elif event.key == pygame.K_DOWN:
                        OPTIONS["mand_loc"]["y"] += 0.25
                        mand_changed = True
                    elif event.key == pygame.K_LEFT:
                        OPTIONS["mand_loc"]["x"] -= 0.25
                        mand_changed = True
                    elif event.key == pygame.K_RIGHT:
                        OPTIONS["mand_loc"]["x"] += 0.25
                        mand_changed = True
                    elif event.key == pygame.K_w:
                        OPTIONS["mand_loc"]["size"] += 0.5
                        mand_changed = True
                    elif event.key == pygame.K_s:
                        OPTIONS["mand_loc"]["size"] -= 0.5
                        mand_changed = True
                    
                    if mand_changed:
                        # The position changed, drop a new state machine in place to render it
                        engines = []
                        append_mand()
                        print('    "mand_loc": ' + json.dumps(OPTIONS["mand_loc"]) + ",")

                elif event.type == pygame.KEYUP and event.key == pygame.K_ESCAPE:
                    running = False
        elif len(engines) == 0:
            # All done with work, so nothing left to do
            break

        if len(engines) > 0:
            # After some period of time, stop working the state machines to give the GUI a chance to update
            work_period = time.time() + (0.1 if OPTIONS["view_only"] else 0.25)
            while ((OPTIONS["show_gui"] and time.time() < work_period) or (not OPTIONS["show_gui"])) and len(engines) > 0:
                # Call into the state machine, it'll return something if state needs updating, otherwise
                # when it's done, it'll just return None, which means we can move on to the next state machine
                job = next(engines[0])
                if job is None:
                    engines.pop(0)
                elif job['type'] == 'animate':
                    break # Just called to give the GUI a chance to refresh
                elif job['type'] == 'set_target':
                    handle_set_target(state, job)
                elif job['type'] == 'dupe_frame':
                    handle_dupe_frame(state, job)
                elif job['type'] == 'save_frame':
                    handle_save_frame(state, job)
                elif job['type'] == 'msg':
                    # A simple message, either update the caption if we have on, or just dump to stdout
                    if OPTIONS["show_gui"]:
                        pygame.display.set_caption(job['msg'])
                    else:
                        if not job.get("info", False):
                            show_msg(job['msg'])
                elif job['type'] == 'draw_pool':
                    # Draw the pool in GUI mode, otherwise, ditch this data
                    if OPTIONS["show_gui"]:
                        state.screen.set_at((job['x'] // OPTIONS["gui_shrink"], job['y'] // OPTIONS["gui_shrink"]), job['rgb'])
                elif job['type'] == "show_loc":
                    # Little helper to draw a "location" when finding the edge
                    for (x, y), rgb in pointer.items():
                        state.screen.set_at((x // OPTIONS["gui_shrink"], y // OPTIONS["gui_shrink"]), rgb)
                    pointer.clear()

                    if job['status'] == 'show':
                        size = 4
                        pt_x, pt_y = mand_to_gui(job['x'], job['y'])
                        pt_x, pt_y = int(pt_x), int(pt_y)
                        for xo in range(-size, size+1):
                            for yo in range(-size, size+1):
                                if xo*xo+yo*yo <= size*size:
                                    if (pt_x + xo, pt_y + yo) not in pointer:
                                        pointer[(pt_x + xo, pt_y + yo)] = state.pixels[pt_y + yo, pt_x + xo]
                                        state.screen.set_at(((pt_x + xo) // OPTIONS["gui_shrink"], (pt_y + yo) // OPTIONS["gui_shrink"]), (255, 64, 64))
                elif job['type'] == 'draw_edge':
                    # Draw the edge that we plan to animate
                    # The first time it's called, dump out that edge to disk
                    if OPTIONS["show_gui"]:
                        size = 4
                        pt_x, pt_y = mand_to_gui(job['x'], job['y'])
                        pt_x, pt_y = int(pt_x), int(pt_y)
                        for xo in range(-size, size+1):
                            for yo in range(-size, size+1):
                                if xo*xo+yo*yo <= size*size:
                                    state.screen.set_at(((pt_x + xo) // OPTIONS["gui_shrink"], (pt_y + yo) // OPTIONS["gui_shrink"]), job['rgb'])
                    if show_border == 0 and OPTIONS["save_results"]:
                        with open("frame_preview.jsonl", "wt") as f:
                            for row in state.preview:
                                f.write(json.dumps(row) + "\n")
                    show_border += 1
                    if show_border % 10 == 1 or True:
                        args_mand = {
                            'alias': 1 if OPTIONS["quick_mode"] else 2, 
                            'size': 5, 
                            'center_x': 0, 
                            'center_y': 0, 
                            'julia': [job['x'], job['y']], 
                            'max_iters': OPTIONS["julia_iters"], 
                            'max_skip': 8 if OPTIONS["quick_mode"] else 0,
                        }
                        args_set = {
                            'x': job['x'],
                            'y': job['y'],
                        }
                        fn = f"frame_{frame_number:05d}.png"
                        frame_number += 1
                        row = {"cmd": "draw", "mand": args_mand, "set": args_set, "dest": fn}
                        if OPTIONS["draw_julias"]:
                            add_frame(engines, row)
                        if OPTIONS["save_results"]:
                            with open("frames.jsonl", "at") as f:
                                f.write(json.dumps(row) + "\n")

                        dupes = 0
                        if OPTIONS["add_extra_frames"]:
                            if job["first"]:
                                dupes = OPTIONS["frame_rate"] - 1
                            elif job["last"]:
                                dupes = (OPTIONS["frame_rate"] * 5) - 1
                        
                        if dupes > 0:
                            source_fn = fn
                            for _ in range(dupes):
                                fn = f"frame_{frame_number:05d}.png"
                                frame_number += 1
                                row = {"cmd": "dupe", "source": source_fn, "dest": fn, "requires": source_fn}
                                add_frame(engines, row)
                                if OPTIONS["save_results"]:
                                    with open("frames.jsonl", "at") as f:
                                        f.write(json.dumps(row) + "\n")
                elif job['type'] == 'draw_mand':
                    handle_draw_mand(state, job)

            if OPTIONS["show_gui"]:
                pygame.display.flip()
                pygame.display.update()

if __name__ == "__main__":
    show_flags()
    if OPTIONS["multiproc"]:
        main_multiproc()
    else:
        main()