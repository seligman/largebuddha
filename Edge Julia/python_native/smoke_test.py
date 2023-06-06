#!/usr/bin/env python3

import mandelbrot_native_helper

in_set, escaped_at, dist = mandelbrot_native_helper.calc(0, 0, False, 0, 0, 1000)
if in_set != 1:
    raise Exception()
in_set, escaped_at, dist = mandelbrot_native_helper.calc(-4, -4, False, 0, 0, 1000)
if in_set != 0:
    raise Exception()

print("Smoke test passed!")
