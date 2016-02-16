# Nebulabrot

This is the worker to create the large-scale buddhabrot.  The final result of these programs are [seen here](nebula.scottandmichelle.net/nebula/index.html).  You can view lots of more information about the Buddhabrot on [Melinda Green's website](http://superliminal.com/fractals/) or on [Wikipedia](https://en.wikipedia.org/wiki/Buddhabrot).

There are three main programs.  See "Notes.txt" for more details on each.

### Threads

This is the main worker that calculates the fractal.

### Create Worker

Simple utility script to create a batch file to run Threads according to various settings.

### Visualize Array

This takes the output from Threads, splits up and scales the data into lots of little .png files for the final wepage.
