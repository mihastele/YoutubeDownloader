const ytdlp = require('yt-dlp-exec');
const ffmpeg = require('fluent-ffmpeg');
const fs = require('fs-extra');
const path = require('path');

async function downloadChannel(channelUrl) {
  // Extract channel name from URL, e.g., https://www.youtube.com/@ChannelName -> ChannelName
  const channelMatch = channelUrl.match(/@([^\/]+)/);
  const channelName = channelMatch ? channelMatch[1] : 'UnknownChannel';
  const outputDir = path.join(__dirname, 'downloads', channelName);

  await fs.ensureDir(outputDir);

  console.log(`Downloading videos from ${channelUrl} to ${outputDir}`);

  // Download best video for each video in channel
  await ytdlp(channelUrl, {
    output: path.join(outputDir, '%(title)s_video.%(ext)s'),
    format: 'bestvideo[ext=mp4]/bestvideo',
    yesPlaylist: true,
    noCheckCertificates: true, // Sometimes needed
  });

  // Download best audio for each video in channel
  await ytdlp(channelUrl, {
    output: path.join(outputDir, '%(title)s_audio.%(ext)s'),
    format: 'bestaudio[ext=m4a]/bestaudio',
    yesPlaylist: true,
    noCheckCertificates: true,
  });

  console.log('Download complete. Starting conversion to MP4...');

  // Get list of video files
  const files = await fs.readdir(outputDir);
  const videoFiles = files.filter(f => f.includes('_video.'));

  for (const videoFile of videoFiles) {
    const baseName = videoFile.replace('_video.mp4', '').replace('_video.webm', '').replace('_video.mkv', '');
    const audioFile = `${baseName}_audio.m4a` || `${baseName}_audio.webm`; // Adjust if needed
    const outputFile = `${baseName}.mp4`;

    const videoPath = path.join(outputDir, videoFile);
    const audioPath = path.join(outputDir, audioFile);
    const outputPath = path.join(outputDir, outputFile);

    if (await fs.pathExists(audioPath)) {
      await new Promise((resolve, reject) => {
        ffmpeg()
          .input(videoPath)
          .input(audioPath)
          .output(outputPath)
          .videoCodec('copy') // Copy to avoid re-encoding if possible
          .audioCodec('copy')
          .on('end', () => {
            console.log(`Converted ${baseName} to MP4`);
            resolve();
          })
          .on('error', reject)
          .run();
      });

      // Clean up temp files
      await fs.remove(videoPath);
      await fs.remove(audioPath);
    } else {
      console.log(`Audio file not found for ${baseName}, skipping conversion`);
    }
  }

  console.log('All conversions complete.');
}

// Usage: node index.js <channel_url>
const channelUrl = process.argv[2];
if (!channelUrl) {
  console.log('Usage: node index.js <YouTube channel URL>');
  process.exit(1);
}

downloadChannel(channelUrl).catch(console.error);
