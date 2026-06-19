namespace ErsatzTV.Core.MediaSegments;

/// <summary>
/// Pure logic that converts a media duration plus a set of Jellyfin media segments
/// and a skip policy into the ordered list of playback ranges that should air.
///
/// Design goals:
///  - Deterministic and side-effect free (easy to unit test).
///  - Safe by default: any missing/invalid data or disabled policy results in a
///    single full-length range (full fallback), so playback is never broken.
/// </summary>
public static class MediaSegmentRangePlanner
{
    public static IReadOnlyList<PlaybackRange> PlanRanges(
        TimeSpan mediaDuration,
        IReadOnlyList<MediaSegment> segments,
        MediaSegmentSkipPolicy policy)
    {
        IReadOnlyList<PlaybackRange> fullPlayback = [new PlaybackRange(TimeSpan.Zero, mediaDuration)];

        // fallback to full playback on bad duration, no policy, disabled policy, or no segments
        if (mediaDuration <= TimeSpan.Zero || policy is null || !policy.HasAnySkip || segments is null ||
            segments.Count == 0)
        {
            return fullPlayback;
        }

        // only consider segments whose type is being skipped
        var active = segments.Where(s => policy.ShouldSkip(s.Type)).ToList();
        if (active.Count == 0)
        {
            return fullPlayback;
        }

        // fallback to full playback if any active segment is invalid (bad data)
        if (active.Any(s => !IsUsableSegment(s, mediaDuration, policy)))
        {
            return fullPlayback;
        }

        // convert active segments into skip intervals, honoring cold-open preservation
        var skips = new List<(TimeSpan Start, TimeSpan End)>();
        foreach (MediaSegment segment in active)
        {
            TimeSpan start = segment.Start;
            TimeSpan end = segment.End > mediaDuration ? mediaDuration : segment.End;

            bool isIntro = segment.Type == MediaSegmentType.Intro;
            bool hasColdOpen = start >= policy.MinimumColdOpen;

            if (isIntro && !(policy.PreserveColdOpen && hasColdOpen))
            {
                // collapse any pre-intro content into the skip when not preserving cold open
                start = TimeSpan.Zero;
            }

            skips.Add((start, end));
        }

        List<(TimeSpan Start, TimeSpan End)> merged = MergeIntervals(skips);
        List<PlaybackRange> ranges = Complement(mediaDuration, merged);

        // if everything was skipped, fall back to full playback rather than airing nothing
        return ranges.Count == 0 ? fullPlayback : ranges;
    }

    private static bool IsUsableSegment(
        MediaSegment segment,
        TimeSpan mediaDuration,
        MediaSegmentSkipPolicy policy)
    {
        if (!segment.IsValid(mediaDuration))
        {
            return false;
        }

        TimeSpan duration = segment.Duration;
        return segment.Type switch
        {
            MediaSegmentType.Intro => duration >= policy.MinIntroDuration &&
                                      duration <= policy.MaxIntroDuration,
            MediaSegmentType.Outro => duration >= policy.MinOutroDuration &&
                                      duration <= policy.MaxOutroDuration &&
                                      segment.Start.TotalMilliseconds / mediaDuration.TotalMilliseconds >=
                                      policy.MinOutroStartPercent,
            _ => true
        };
    }

    private static List<(TimeSpan Start, TimeSpan End)> MergeIntervals(List<(TimeSpan Start, TimeSpan End)> intervals)
    {
        var ordered = intervals.OrderBy(i => i.Start).ToList();
        var merged = new List<(TimeSpan Start, TimeSpan End)>();

        foreach ((TimeSpan Start, TimeSpan End) current in ordered)
        {
            if (merged.Count == 0)
            {
                merged.Add(current);
                continue;
            }

            (TimeSpan Start, TimeSpan End) last = merged[^1];
            if (current.Start <= last.End)
            {
                // overlapping or adjacent: extend the previous interval
                merged[^1] = (last.Start, current.End > last.End ? current.End : last.End);
            }
            else
            {
                merged.Add(current);
            }
        }

        return merged;
    }

    private static List<PlaybackRange> Complement(
        TimeSpan duration,
        List<(TimeSpan Start, TimeSpan End)> skips)
    {
        var ranges = new List<PlaybackRange>();
        TimeSpan cursor = TimeSpan.Zero;

        foreach ((TimeSpan Start, TimeSpan End) skip in skips)
        {
            if (skip.Start > cursor)
            {
                ranges.Add(new PlaybackRange(cursor, skip.Start));
            }

            if (skip.End > cursor)
            {
                cursor = skip.End;
            }
        }

        if (cursor < duration)
        {
            ranges.Add(new PlaybackRange(cursor, duration));
        }

        return ranges;
    }
}
