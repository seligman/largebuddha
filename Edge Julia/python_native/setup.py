#!/usr/bin/env python3

from distutils.core import setup, Extension

def main():
    setup(name="mandelbrot_native_helper",
          version="1.0.0",
          description="Native Mandelbrot Helper",
          author="Scott Seligman",
          author_email="scott.seligman@gmail.com",
          ext_modules=[Extension("mandelbrot_native_helper", ["mandelbrot_native_helper.c"])])

if __name__ == "__main__":
    main()
