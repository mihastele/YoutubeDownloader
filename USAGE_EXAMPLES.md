# Usage Examples for New Features

## Smart Duplicate Detection

### Scenario: Downloading a YouTube Channel
When downloading all videos from a channel, some videos might already exist in your target directory.

**Before (old behavior):**
- Would attempt to download all videos
- Could overwrite existing files
- No validation of existing file integrity

**After (new behavior):**
```
1. User selects channel videos for download
2. System checks target directory
3. For each video:
   - Check if file exists
   - Validate file integrity (size, headers)
   - Skip if valid file exists
   - Queue for download if missing or corrupted
```

**Result:** Only downloads missing or corrupted videos, saving time and bandwidth.

## Adaptive Throttling

### Scenario: Rate Limited Downloads
When downloading many videos, you might encounter rate limiting or temporary failures.

**Without Throttling:**
```
Download 1: ✓ Success
Download 2: ✗ Fail (rate limited)
Download 3: ✗ Fail (rate limited)
Download 4: ✗ Fail (rate limited)
Download 5: ✗ Fail (rate limited)
...continues failing
```

**With Throttling Enabled:**
```
Download 1: ✓ Success
Download 2: ✗ Fail (rate limited)
  → Throttling increased to 1.0s
Download 3: [wait 1.0s] ✗ Fail
  → Throttling increased to 2.0s
Download 4: [wait 2.0s] ✗ Fail
  → Throttling increased to 4.0s
Download 5: [wait 4.0s] ✓ Success
Download 6: [wait 4.0s] ✓ Success
Download 7: [wait 4.0s] ✓ Success
  → Throttling reduced to 3.0s (after 3 successes)
Download 8: [wait 3.0s] ✓ Success
...continues reducing delay as downloads succeed
```

**Result:** Automatically adapts to rate limiting, ensuring downloads eventually succeed.

## UI Workflow

### Multiple Download Setup
1. Enter YouTube playlist/channel URL
2. Select videos to download
3. Choose format and quality
4. **NEW:** Check "Enable throttling" checkbox
5. Click "DOWNLOAD"
6. System applies smart duplicate detection
7. Downloads proceed with adaptive throttling if enabled

### Single Download Setup
1. Enter YouTube video URL
2. Choose format and quality
3. **NEW:** Check "Enable throttling" checkbox
4. Click "DOWNLOAD"
5. Download proceeds with throttling if enabled

### Status Indicators
- **During Download:** Shows "Throttling active - waiting X.Xs" when delays are applied
- **In Debug Output:** Shows throttling adjustments and reasoning
- **Settings:** Throttling preference is remembered between sessions

## Code Integration Points

### For Developers
The new features integrate seamlessly with existing code:

```csharp
// VideoFileValidator usage
bool isValid = await VideoFileValidator.IsValidVideoFileAsync(filePath);

// AdaptiveThrottleManager usage
var throttleManager = new AdaptiveThrottleManager();
throttleManager.IsEnabled = true;

// Before download
await throttleManager.WaitAsync();

// After successful download
throttleManager.ReportSuccess();

// After failed download
throttleManager.ReportFailure();
```

### Settings Integration
```csharp
// Settings are automatically persisted
settingsService.IsThrottlingEnabled = true; // Saved automatically
```

## Best Practices

### When to Enable Throttling
- ✅ Downloading large batches (playlists, channels)
- ✅ Experiencing rate limiting
- ✅ Unreliable internet connection
- ✅ Downloading from busy/popular sources

### When Throttling Might Not Be Needed
- ❌ Single video downloads
- ❌ Fast, reliable connection
- ❌ Small batches (< 5 videos)
- ❌ When speed is critical

### File Validation Benefits
- ✅ Always beneficial - no downsides
- ✅ Prevents wasted bandwidth
- ✅ Ensures file integrity
- ✅ Saves time on re-downloads
