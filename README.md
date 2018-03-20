Extracts the still image and optionally video from a Motion Picture shot with a Pixel2 XL.

Easiest way to use: Double click it and it extracts the main image from all JPG files in the same directory (any non-motion photos will just get duplicated) into a file with the same filename and the suffix "_still".
There are a few parameters this can be launched with (e.g. with a shortcut, from the CMD or with a batch file):

- `/?`: Prints a help
- `/a`: Advanced mode. This flag by itself does nothing much. It uses the JPG headers to determine the still image (Normal mode just uses C# APIs to open and save the image again)
- `/l`: Works only with advanced mode. Prints a log file of the contens of the header
- `/v`: Also extracts the embedded video
