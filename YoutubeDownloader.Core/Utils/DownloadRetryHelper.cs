using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace YoutubeDownloader.Core.Utils;

/// <summary>
/// Helper to retry a single download operation with immediate retries and adaptive backoff.
/// - Retries the same download before moving to the next item.
/// - Supports cancellation (use for a manual "Skip" button).
/// - Reports backoff delays via onBackoff callback so UI can show delay countdown.
/// </summary>
public static class DownloadRetryHelper
{
    private static HashSet<string> downloadedVideos = new HashSet<string>();

    /// <summary>
    /// Tries to run downloadFunc until success, cancellation, or maxRetries reached.
    /// downloadFunc should return true on success, false on transient failure.
    /// If provided, validateDownloadedFunc will be called after a download reports success; it should return
    /// true when the downloaded file is valid. If the validator returns false the helper treats the
    /// attempt as a failure (validator is responsible for deleting or fixing the invalid file if needed).
    /// onBackoff is invoked when a delay is applied (use to update UI).
    /// </summary>
    public static async Task<bool> TryDownloadWithRetriesAsync(
        Func<CancellationToken, Task<bool>> downloadFunc,
        CancellationToken cancellationToken,
        int immediateRetries = 2,
        int maxRetries = 8,
        bool enableThrottling = true,
        Action<TimeSpan>? onBackoff = null,
        // Optional: validate the downloaded file after downloadFunc reports success.
        // Should return true if the file is valid. If validation fails the helper will treat
        // the attempt as a failure and retry (validator may delete the invalid file).
        Func<CancellationToken, Task<bool>>? validateDownloadedFunc = null,
        // Optional: notified when validation fails (use to update UI or log)
        Action? onValidationFailed = null
    )
    {
        int attempts = 0;
        int consecutiveFailures = 0;

        while (!cancellationToken.IsCancellationRequested && attempts <= maxRetries)
        {
            attempts++;
            var success = await downloadFunc(cancellationToken).ConfigureAwait(false);

            // If download reported success, optionally validate the downloaded file.
            if (success)
            {
                if (validateDownloadedFunc != null)
                {
                    var valid = false;
                    try
                    {
                        valid = await validateDownloadedFunc(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation during validation -> stop retry loop
                        break;
                    }
                    catch
                    {
                        // If validator throws, treat as invalid so we retry
                        valid = false;
                    }

                    if (!valid)
                    {
                        // Let caller know validation failed (e.g., to delete file / show UI)
                        try
                        {
                            onValidationFailed?.Invoke();
                        }
                        catch { }

                        // treat validation failure as a retryable failure
                        consecutiveFailures++;
                        // continue to backoff/retry
                    }
                    else
                    {
                        // On success + valid, decay failure count and return
                        consecutiveFailures = Math.Max(0, consecutiveFailures - 1);
                        return true;
                    }
                }
                else
                {
                    // No validator provided: accept success
                    consecutiveFailures = Math.Max(0, consecutiveFailures - 1);
                    return true;
                }
            }

            consecutiveFailures++;

            // If we've reached max attempts, stop
            if (attempts > maxRetries)
                break;

            // Determine delay:
            TimeSpan delay;
            if (attempts <= immediateRetries)
            {
                // small immediate retry
                delay = TimeSpan.FromMilliseconds(500);
            }
            else if (enableThrottling)
            {
                // exponential backoff with cap (1s * 2^(failures-1))
                var secs = Math.Min(200, Math.Pow(2, consecutiveFailures - 1));
                delay = TimeSpan.FromSeconds(secs);
            }
            else
            {
                delay = TimeSpan.FromSeconds(1);
            }

            onBackoff?.Invoke(delay);

            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return false;
    }

    public static bool TryDownload(string videoId)
    {
        // Check if the video has already been downloaded
        if (downloadedVideos.Contains(videoId))
        {
            return false; // Skip downloading
        }

        // Proceed with the download logic...
        // After successful download, add to the list
        downloadedVideos.Add(videoId);
        return true; // Indicate success
    }
}
