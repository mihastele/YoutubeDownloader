const ytdlp = require('yt-dlp-exec');
const fs = require('fs-extra');
const path = require('path');

async function downloadChannel(channelUrl) {
  // Extract channel name from URL, e.g., https://www.youtube.com/@ChannelName -> ChannelName
  const channelMatch = channelUrl.match(/@([^\/]+)/);
  const channelName = channelMatch ? channelMatch[1] : 'UnknownChannel';
  const outputDir = path.join(__dirname, 'downloads', channelName);

  await fs.ensureDir(outputDir);

  console.log(`Downloading videos from ${channelUrl} to ${outputDir}`);

  // Download best video+audio merged to MP4 for each video in channel
  await ytdlp(channelUrl, {
    output: path.join(outputDir, '%(title)s.%(ext)s'),
    format: 'bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best',
    remuxVideo: 'mp4',
    yesPlaylist: true,
    noCheckCertificates: true,
  });

  console.log('Download and conversion to MP4 complete.');
}

// Usage: node index.js <channel_url>
const channelUrl = process.argv[2];
if (!channelUrl) {
  console.log('Usage: node index.js <YouTube channel URL>');
  process.exit(1);
}

downloadChannel(channelUrl).catch(console.error);
