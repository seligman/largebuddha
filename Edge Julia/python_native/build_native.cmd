@echo off

python3 setup.py build

if errorlevel 1 (
    echo Build failed
    goto :EOF
)

python3 -m pip install --user .

python3 smoke_test.py
