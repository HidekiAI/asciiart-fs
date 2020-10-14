
# libAA-FS (ASCII Art F#)

## Summary:
* project that can be written quick yet "fun" for learning F#
*

## What have I learned:
* Trust unit-test, not the library which they may claim "stable"; gdi returns width +1
* Array.zeroCreate is your friend; appending to array caused almost infinite loop; for a 640x480 image, it would start off fast, but as it it got about 1/3 scanline, it started to get slower and slower; profiled for memory bloat (not for leaks), etc but no increase in memory was observed; Final assumptions made is that GC cannot keep up, so decided to preallocate and update each pixels 

## Todo:
* Parse arg for in-file and out-file
* optimize
* get ANSI-color working

## Running and Tests
* dotnet test
* run.sh provided for debug runs

![Screenshot](Screenshot.png)

## Things I'd like to try:
* Use Convolution to determine the 4x4 and 8x8 blocks
* Use imagemagik or some existing "good" libraries that already can convert images to ASCII and make a pool of mapping of converted data, and have tensorflow learn what other libraries have used for 4x4 or 8x8 block to an ASCII char; use that as the block char lookup
* Loop an image to generate ASCII art, have tensorflow compare, and then guess on a new ASCII lookup map, generate again, and so on, until a good ASCII lookup map can be generated
