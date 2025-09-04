using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeDownloader.Core.Utils;

/// <summary>
/// Manages adaptive throttling for downloads with automatic delay adjustment based on success/failure rates
/// </summary>
public class AdaptiveThrottleManager : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private TimeSpan _currentDelay = TimeSpan.Zero;
    private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _maxDelay = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _minDelay = TimeSpan.Zero;

    private int _consecutiveFailures = 0;
    private int _consecutiveSuccesses = 0;
    private DateTimeOffset _lastRequestTime = DateTimeOffset.MinValue;

    private bool _isEnabled;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public TimeSpan CurrentDelay => _currentDelay;

    /// <summary>
    /// Waits according to the current throttling delay
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _currentDelay == TimeSpan.Zero)
            return;

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var timeSinceLastRequest = DateTimeOffset.Now - _lastRequestTime;
            var remainingDelay = _currentDelay - timeSinceLastRequest;

            if (remainingDelay > TimeSpan.Zero)
            {
                await Task.Delay(remainingDelay, cancellationToken);
            }

            _lastRequestTime = DateTimeOffset.Now;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Reports a successful download operation
    /// </summary>
    public void ReportSuccess()
    {
        if (!_isEnabled)
            return;

        _consecutiveFailures = 0;
        _consecutiveSuccesses++;

        // After 3 consecutive successes, start reducing delay
        if (_consecutiveSuccesses >= 3)
        {
            ReduceDelay();
            _consecutiveSuccesses = 0; // Reset counter after adjustment
        }
    }

    /// <summary>
    /// Reports a failed download operation
    /// </summary>
    public void ReportFailure()
    {
        if (!_isEnabled)
            return;

        _consecutiveSuccesses = 0;
        _consecutiveFailures++;

        // Increase delay immediately on failure
        IncreaseDelay();
    }

    /// <summary>
    /// Resets the throttling state
    /// </summary>
    public void Reset()
    {
        _currentDelay = TimeSpan.Zero;
        _consecutiveFailures = 0;
        _consecutiveSuccesses = 0;
    }

    private void IncreaseDelay()
    {
        if (_currentDelay == TimeSpan.Zero)
        {
            _currentDelay = _initialDelay;
        }
        else
        {
            // Double the delay, but cap it at max delay
            var newDelay = TimeSpan.FromMilliseconds(_currentDelay.TotalMilliseconds * 2);
            _currentDelay = newDelay > _maxDelay ? _maxDelay : newDelay;
        }

        Debug.WriteLine($"Throttling increased to {_currentDelay.TotalSeconds:F1}s after failure");
    }

    private void ReduceDelay()
    {
        if (_currentDelay > _minDelay)
        {
            // Reduce delay by 25%
            var newDelay = TimeSpan.FromMilliseconds(_currentDelay.TotalMilliseconds * 0.75);
            _currentDelay = newDelay < _minDelay ? _minDelay : newDelay;

            Debug.WriteLine($"Throttling reduced to {_currentDelay.TotalSeconds:F1}s after consecutive successes");
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
