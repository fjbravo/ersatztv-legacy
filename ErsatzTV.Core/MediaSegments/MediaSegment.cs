namespace ErsatzTV.Core.MediaSegments;

/// <summary>
/// A single media segment (e.g. an intro or outro) reported by Jellyfin,
/// expressed as a start/end offset into the media item.
/// </summary>
public record MediaSegment(MediaSegmentType Type, TimeSpan Start, TimeSpan End)
{
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// A segment is structurally valid when it falls within the media duration and
    /// has a positive length. End may equal the media duration (e.g. an outro that
    /// runs to the very end). End slightly beyond the duration is tolerated by the
    /// planner via clamping; here we only reject clearly nonsensical segments.
    /// </summary>
    public bool IsValid(TimeSpan mediaDuration)
    {
        if (mediaDuration <= TimeSpan.Zero)
        {
            return false;
        }

        if (Start < TimeSpan.Zero)
        {
            return false;
        }

        if (End <= Start)
        {
            return false;
        }

        // a segment that begins at or after the end of the media is meaningless
        if (Start >= mediaDuration)
        {
            return false;
        }

        return true;
    }
}
