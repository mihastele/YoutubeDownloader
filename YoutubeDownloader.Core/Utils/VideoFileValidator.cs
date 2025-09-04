using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeDownloader.Core.Utils;

public static class VideoFileValidator
{
    /// <summary>
    /// Validates if a video file is corrupted by checking basic file integrity
    /// </summary>
    /// <param name="filePath">Path to the video file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file appears to be valid, false if corrupted or invalid</returns>
    public static async Task<bool> IsValidVideoFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            var fileInfo = new FileInfo(filePath);

            // Check if file is empty or too small to be a valid video
            if (fileInfo.Length < 1024) // Less than 1KB is likely corrupted
                return false;

            // Check if file can be opened and read
            using var stream = File.OpenRead(filePath);

            // Try to read a few bytes to ensure file is not locked or corrupted
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (bytesRead == 0)
                return false;

            // Basic validation for common video file headers
            return ValidateFileHeader(buffer, Path.GetExtension(filePath).ToLowerInvariant());
        }
        catch (Exception)
        {
            // If we can't read the file, consider it corrupted
            return false;
        }
    }

    private static bool ValidateFileHeader(byte[] buffer, string extension)
    {
        if (buffer.Length < 8)
            return false;

        return extension switch
        {
            ".mp4" => ValidateMp4Header(buffer),
            ".webm" => ValidateWebMHeader(buffer),
            ".mp3" => ValidateMp3Header(buffer),
            ".ogg" => ValidateOggHeader(buffer),
            _ => true // For unknown extensions, assume valid if we can read the file
        };
    }

    private static bool ValidateMp4Header(byte[] buffer)
    {
        // MP4 files typically start with specific byte patterns
        // Check for 'ftyp' box which should be near the beginning
        for (int i = 4; i < buffer.Length - 4; i++)
        {
            if (buffer[i] == 0x66 && buffer[i + 1] == 0x74 &&
                buffer[i + 2] == 0x79 && buffer[i + 3] == 0x70) // 'ftyp'
                return true;
        }
        return false;
    }

    private static bool ValidateWebMHeader(byte[] buffer)
    {
        // WebM files start with EBML header
        if (buffer.Length >= 4)
        {
            return buffer[0] == 0x1A && buffer[1] == 0x45 &&
                   buffer[2] == 0xDF && buffer[3] == 0xA3;
        }
        return false;
    }

    private static bool ValidateMp3Header(byte[] buffer)
    {
        // MP3 files can start with ID3 tag or directly with frame header
        if (buffer.Length >= 3)
        {
            // Check for ID3 tag
            if (buffer[0] == 0x49 && buffer[1] == 0x44 && buffer[2] == 0x33) // 'ID3'
                return true;

            // Check for MP3 frame header (sync word)
            if ((buffer[0] == 0xFF) && ((buffer[1] & 0xE0) == 0xE0))
                return true;
        }
        return false;
    }

    private static bool ValidateOggHeader(byte[] buffer)
    {
        // Ogg files start with 'OggS'
        if (buffer.Length >= 4)
        {
            return buffer[0] == 0x4F && buffer[1] == 0x67 &&
                   buffer[2] == 0x67 && buffer[3] == 0x53; // 'OggS'
        }
        return false;
    }
}
