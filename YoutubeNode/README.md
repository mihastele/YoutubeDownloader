# YouTube Channel Downloader

A Node.js application to download all videos from a YouTube channel at the highest quality and convert them to MP4 using FFmpeg.

## Features

- Downloads entire YouTube channels
- Downloads videos and audio at highest quality
- Merges video and audio into MP4 format using FFmpeg
- Server-friendly and graceful handling

## Installation

1. Clone or download the project.
2. Run `npm install` to install dependencies.

## Usage

Run the application with a YouTube channel URL:

```
node index.js https://www.youtube.com/@ChannelName
```

Replace `https://www.youtube.com/@ChannelName` with the actual channel URL.

Videos will be downloaded to `./downloads/ChannelName/`.

## Requirements

- Node.js
- yt-dlp (installed via npm)
- FFmpeg (used via fluent-ffmpeg)

## Note

Ensure yt-dlp and FFmpeg are available in your system PATH or installed properly.


## TODO Download audio and merge