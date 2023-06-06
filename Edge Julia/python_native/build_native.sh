#!/bin/bash

set -e

python3 setup.py build
python3 -m pip install --user .
python3 smoke_test.py
