#!/usr/bin/env python3

from collections import defaultdict
from PIL import Image, ImageDraw, ImageFont
from scottsutils.command_opts import opt, main_entry
import mandelbrot_native_helper
import math
import numpy as np
import os
import pickle
import subprocess

def val_to_color(val):
    colors = [
        (0, 1, (0, 0, 0), (0, 0, 100)),
        (1, 15, (0, 0, 100), (255, 255, 255)),
        (15, 50, (255, 255, 255), (255, 180, 0)),
        (50, 250, (255, 180, 0), (100, 0, 0)),
    ]
    for a, b, rgb_a, rgb_b in colors:
        if a <= val <= b:
            val = (val - a) / (b - a)
            return (
                int(rgb_a[0] * (1 - val) + rgb_b[0] * val),
                int(rgb_a[1] * (1 - val) + rgb_b[1] * val),
                int(rgb_a[2] * (1 - val) + rgb_b[2] * val),
            )

def calc_point(x, y, image_size, iters=250, julia=False, julia_pt=[0,0]):
    alias_size = 8
    hits = 0
    total = 0
    for ox in range(alias_size):
        for oy in range(alias_size):
            if julia:
                px = ((x + ox / alias_size) / image_size) * 3 - 1.5
                py = ((y + oy / alias_size) / image_size) * 3 - 1.5
            else:
                px = ((x + ox / alias_size) / image_size) * 3 - 2.25
                py = ((y + oy / alias_size) / image_size) * 3 - 1.5
            in_set, escaped_at, final_dist = mandelbrot_native_helper.calc(px, py, julia, julia_pt[0], julia_pt[1], iters)
            total += 1
            if in_set:
                hits += 0
            else:
                hits += escaped_at
    return hits / total

@opt("Draw a Mandelbrot to a Julia")
def mand_to_julia():
    if not os.path.isdir("images"):
        os.mkdir("images")

    fract_size = 300
    julia_pt = (-0.51, 0.52)

    border = 10
    bits = np.zeros([fract_size + border * 2, fract_size * 2 + border * 3, 3], dtype=np.uint8)

    for y in range(fract_size):
        for x in range(fract_size):
            val = calc_point(x, y, fract_size)
            bits[y + border, x + border] = val_to_color(val)

    for y in range(fract_size):
        for x in range(fract_size):
            val = calc_point(x, y, fract_size, julia=True, julia_pt=julia_pt)
            bits[y + border, x + fract_size + border * 2] = val_to_color(val)

    dot_size, border_size = 4, 5
    alias_size = 4
    totals = defaultdict(int)
    borders = defaultdict(int)
    hits = defaultdict(int)

    for y in range(-(border_size + 1) * alias_size, (border_size + 2) * alias_size):
        for x in range(-(border_size + 1) * alias_size, (border_size + 2) * alias_size):
            dist = math.sqrt((x / alias_size) ** 2 + (y / alias_size) ** 2)
            totals[(int(x / alias_size + 100) - 100, int(y / alias_size + 100) - 100)] += 1
            if dist <= dot_size:
                hits[(int(x / alias_size + 100) - 100, int(y / alias_size + 100) - 100)] += 1
            elif dist <= border_size:
                borders[(int(x / alias_size + 100) - 100, int(y / alias_size + 100) - 100)] += 1

    dot_x = int((1/12)*fract_size*(4*julia_pt[0]+9) + border)
    dot_y = int((1/6)*fract_size*(2*julia_pt[1]+3) + border)

    for x, y in totals:
        dot_val = hits[(x, y)] / totals[(x, y)]
        border_val = hits[(x, y)] / totals[(x, y)]
        if val > 0:
            bits[(dot_y + y, dot_x + x)] = (
                int(float(bits[dot_y + y, dot_x + x][0]) * (1 - (dot_val + border_val)) + (255 * dot_val) + (0 * border_val)),
                int(float(bits[dot_y + y, dot_x + x][0]) * (1 - (dot_val + border_val)) + (0 * dot_val) + (0 * border_val)),
                int(float(bits[dot_y + y, dot_x + x][0]) * (1 - (dot_val + border_val)) + (0 * dot_val) + (0 * border_val)),
            )

    im = Image.fromarray(bits)
    im.save(os.path.join("images", "mand_julia.png"))

@opt("Show A* search")
def a_star():
    os.environ['PYGAME_HIDE_SUPPORT_PROMPT'] = "hide"
    import pygame
    from skimage.transform import resize

    for fn in os.listdir("."):
        if fn.startswith("frame_") and fn.endswith(".png"):
            os.unlink(fn)

    alias = 4
    width, height = 500 * alias, 300 * alias
    skip_rate = 100

    screen = pygame.display.set_mode((width // alias, height // alias))
    pygame.display.set_caption('Mandelbrot')
    pygame.display.flip()
    pygame.display.update()

    def is_in_set(x, y):
        in_set, escaped_at, final_dist = mandelbrot_native_helper.calc(
            (((x - (width - height) / 2) / height) - 0.5) * 2.25 - 0.75, 
            ((y / height) - 0.5) * 2.25 + 0, 
            False, 0, 0, 25,
        )
        return in_set == 1
    
    def is_border(x, y):
        if not is_in_set(x, y): return False
        
        if not is_in_set(x - 1, y - 1): return True
        if not is_in_set(x + 1, y - 1): return True
        if not is_in_set(x + 1, y + 1): return True
        if not is_in_set(x - 1, y + 1): return True
        if not is_in_set(x, y - 1): return True
        if not is_in_set(x + 1, y): return True
        if not is_in_set(x, y + 1): return True
        if not is_in_set(x - 1, y): return True

        return False

    at = [350 * alias, 150 * alias]
    mode = "find_start"
    to_check = []
    seen = set()

    seen_pixels = np.zeros((width, height))
    surface = pygame.surfarray.make_surface(np.zeros((width // alias, height // alias, 3), np.uint8))
    skip = 0
    frame = 0

    large_dot, small_dot, border_dot = [], [], []
    for ox in range(-10 * alias, 10 * alias):
        for oy in range(-10 * alias, 10 * alias):
            if math.sqrt(ox ** 2 + oy ** 2) <= 2 * alias:
                border_dot.append((ox, oy))
            if math.sqrt(ox ** 2 + oy ** 2) <= 4 * alias:
                small_dot.append((ox, oy))
            if math.sqrt(ox ** 2 + oy ** 2) <= 6 * alias:
                large_dot.append((ox, oy))

    def draw_screen(seen_pixels, cursor, path):
        nonlocal frame
        bits = np.zeros((width, height, 3), np.uint8)
        temp_cursor = np.zeros((width, height), np.uint8)
        for x, y in large_dot:
            x += cursor[0]
            y += cursor[1]
            if 0 <= x < width and 0 <= y < height:
                temp_cursor[x, y] = 1

        bits[seen_pixels > 0] = (64, 64, 64)
        if path is not None:
            bits[path > 0] = (255, 215, 0)
        bits[temp_cursor > 0] = (192, 192, 255)

        bits = bits.astype(np.float64)
        bits /= 255.0
        dupe = resize(bits, (width // alias, height // alias))
        dupe *= 255.0
        dupe = dupe.astype(np.uint8)

        pygame.pixelcopy.array_to_surface(surface, dupe)
        screen.blit(surface, (0, 0))
        dupe = np.flipud(np.rot90(dupe))
        Image.fromarray(dupe).save(f"frame_{frame:04d}.png")
        frame += 1

    running = True
    while running:
        for event in pygame.event.get():
            if event.type == pygame.KEYUP:
                if event.key == pygame.K_ESCAPE:
                    running = False

        if mode == "find_start":
            if is_border(*at):
                mode = "border_down_only"
                to_check.append((0, at[0], at[1], []))
            else:
                for ox, oy in small_dot:
                    seen_pixels[at[0] + ox, at[1] + oy] = 1
                skip += 1
                if skip % skip_rate == 0:
                    draw_screen(seen_pixels, at, None)
                at[0] += 1
        elif mode in {"border_down_only", "border"}:
            if len(to_check) == 0:
                for _ in range(30):
                    draw_screen(seen_pixels, (-100, -100), temp)
                running = False
            else:
                cost, x, y, path = to_check.pop(0)
                skip += 1
                if skip % skip_rate == 0:
                    temp = np.zeros((width, height))
                    for ox, oy in border_dot:
                        for px, py in path:
                            temp[px + ox, py + oy] = 1
                    draw_screen(seen_pixels, (x, y), temp)
                
                if x <= 93 * alias and mode == "border_down_only":
                    mode = "border"
                    seen = set()
                if is_border(x, y):
                    for dx, dy in [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]:
                        use = True
                        if mode == "border_down_only":
                            if (y + dy) < height // 2:
                                use = False
                        else:
                            if (y + dy) > height // 2:
                                use = False
                        if use:
                            if ((x + dx), (y + dy)) not in seen:
                                seen.add(((x + dx), (y + dy)))
                                for ox, oy in small_dot:
                                    seen_pixels[x + dx + ox, y + dy + oy] = 1
                                to_check.append((cost + 1, (x + dx), (y + dy), path + [(x, y)]))
                    to_check.sort()

        pygame.display.flip()
        pygame.display.update()

    subprocess.check_call([
        "ffmpeg", 
        "-y",
        "-framerate", "25",
        "-i", "frame_%04d.png",
        "-vf", "fps=25,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse",
        os.path.join("images", "a_star.gif"),
    ])


@opt("Find a good Mandelbrot image to use for showing there are gaps")
def zoom_mandelbrot():
    os.environ['PYGAME_HIDE_SUPPORT_PROMPT'] = "hide"
    import pygame

    screen = pygame.display.set_mode((300, 300))
    pygame.display.set_caption('Mandelbrot')
    pygame.display.flip()
    pygame.display.update()

    mand_x, mand_y, mand_size = -0.1875, -0.875, 0.5
    key_split = 1
    running = True

    while running:
        mand_changed = False
        for event in pygame.event.get():
            if event.type == pygame.KEYUP:
                if event.key == pygame.K_ESCAPE:
                    running = False
                elif event.key == pygame.K_z:
                    key_split *= 2
                    print(f"Key split is now {key_split}")
                elif event.key == pygame.K_x:
                    key_split /= 2
                    print(f"Key split is now {key_split}")
                elif event.key == pygame.K_UP:
                    mand_y -= 0.25 / key_split
                    mand_changed = True
                elif event.key == pygame.K_DOWN:
                    mand_y += 0.25 / key_split
                    mand_changed = True
                elif event.key == pygame.K_LEFT:
                    mand_x -= 0.25 / key_split
                    mand_changed = True
                elif event.key == pygame.K_RIGHT:
                    mand_x += 0.25 / key_split
                    mand_changed = True
                elif event.key == pygame.K_w:
                    mand_size += 0.5 / key_split
                    mand_changed = True
                elif event.key == pygame.K_s:
                    mand_size -= 0.5 / key_split
                    mand_changed = True
        if mand_changed:
            print(f"Mandelbrot set to {mand_x}, {mand_y}, {mand_size}")
        for x in range(0, 300):
            for y in range(0, 300):
                in_set, escaped_at, final_dist = mandelbrot_native_helper.calc(
                    ((x / 300) - 0.5) * mand_size + mand_x, 
                    ((y / 300) - 0.5) * mand_size + mand_y, 
                    False, 0, 0, 100,
                )
                if in_set:
                    screen.set_at((x, y), (255, 255, 255))
                else:
                    screen.set_at((x, y), (0, 0, 0))

        pygame.display.flip()
        pygame.display.update()

    if not os.path.isdir("images"):
        os.mkdir("images")

    fract_size = 300
    bits = np.zeros([fract_size, fract_size, 3], dtype=np.uint8)
    for x in range(300):
        for y in range(300):
            hits, total = 0, 0
            for ox in range(4):
                for oy in range(4):
                    in_set, escaped_at, final_dist = mandelbrot_native_helper.calc(
                        (((x + (ox / 4)) / 300) - 0.5) * mand_size + mand_x, 
                        (((y + (oy / 4)) / 300) - 0.5) * mand_size + mand_y, 
                        False, 0, 0, 100,
                    )
                    total += 1
                    if in_set:
                        hits += 1
            val = int(hits / total * 255)
            bits[y, x] = (val, val, val)

    im = Image.fromarray(bits)
    im.save(os.path.join("images", "mandelbrot_detail.png"))

@opt("Animate border iterations")
def animate_border():
    for fn in os.listdir("."):
        if fn.startswith("frame_") and fn.endswith(".png"):
            os.unlink(fn)

    fn = os.path.join("data", "edge_ITER_e05x100.png.dat")
    iters = [
        4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 17, 19, 20, 22, 25, 27, 30, 33, 
        36, 39, 43, 47, 52, 57, 63, 69, 75, 83, 91, 100, 
    ]
        # 109, 120, 131, 144, 
        # 158, 173, 190, 208, 229, 251, 275, 301, 331, 363, 398, 436, 478, 524, 
        # 575, 630, 691, 758, 831, 1000,

    frame_number = 0
    for iter in iters:
        width, height = 400, 300
        bits = np.zeros((height, width, 3), np.uint8)
        alias = 4
        for x in range(width):
            for y in range(height):
                hits, total = 0, 0
                for ox in range(alias):
                    for oy in range(alias):
                        in_set, escaped_at, final_dist = mandelbrot_native_helper.calc(
                            (((x + ox / alias) - (width/2)) / 300 * 2.75 - 0.75), 
                            (((y + oy / alias) - (height/2)) / 300 * 2.75), 
                            False, 0, 0, iter,
                        )
                        if in_set:
                            hits += 1
                        total += 1
                val = int(hits / total * 255)
                bits[y, x] = (val, val, val)

        border = np.zeros((height * alias, width * alias), np.uint8)
        with open(fn.replace("ITER", f"{iter:05d}"), "rb") as f:
            data = pickle.load(f)
        seen = set()
        for pt_x, pt_y in data:
            x = int(((width / 2) + (300 / 11) * (4 * pt_x + 3)) * alias)
            y = int(((height / 2) + ((1200 * pt_y) / 11)) * alias)
            if (x, y) not in seen:
                seen.add((x, y))
                dot_size = 6
                for ox in range(-(dot_size + 1), (dot_size + 2)):
                    for oy in range(-(dot_size + 1), (dot_size + 2)):
                        dist = math.sqrt(ox ** 2 + oy ** 2)
                        if dist <= dot_size:
                            if (0 <= x + ox < width * alias) and (0 <= y + oy < height * alias):
                                border[y+oy, x+ox] = 1

        for x in range(width):
            for y in range(height):
                hits, total = 0, 0
                for ox in range(alias):
                    for oy in range(alias):
                        total += 1
                        hits += border[y * alias + oy, x * alias + ox]
                val = hits / total
                if val > 0:
                    bits[y, x] = (
                        bits[y, x][0] * (1 - val) + 255 * val,
                        bits[y, x][2] * (1 - val) + 0 * val,
                        bits[y, x][1] * (1 - val) + 0 * val,
                    )

        im = Image.fromarray(bits)
        dr = ImageDraw.Draw(im)
        fnt = ImageFont.truetype("SourceSerif4-Medium.ttf", size=20)
        caption = f"{iter:,} iterations"
        size = fnt.getbbox(caption + ",")
        dr.rectangle((width - (size[2] + 15), height - (size[3] + 15), 1000, 1000), (0, 0, 0))
        dr.text((width - (size[2] + 10), height - (size[3] + 10)), caption, (255, 255, 255), fnt)
        im.save(f"frame_{frame_number:04d}.png")
        print(f"Created frame_{frame_number:04d}.png")
        frame_number += 1

    subprocess.check_call([
        "ffmpeg", 
        "-y",
        "-framerate", "2",
        "-i", "frame_%04d.png",
        "-vf", "fps=2,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse",
        os.path.join("images", "border_animated.gif"),
    ])


if __name__ == "__main__":
    main_entry('func')
