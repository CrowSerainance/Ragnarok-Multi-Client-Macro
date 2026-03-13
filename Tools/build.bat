@echo off
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x86
cd /d "%~dp0\.."
mkdir build 2>nul
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
