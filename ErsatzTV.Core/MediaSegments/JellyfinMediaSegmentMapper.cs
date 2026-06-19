namespace ErsatzTV.Core.MediaSegments;

/// <summary>
/// Pure mapping from Jellyfin's media segment vocabulary (type name + tick offsets)
/// into the core <see cref="MediaSegment"/> model. Kept free of any Jellyfin DTO or
/// HTTP types so it can be unit tested in isolation.
/// </summary>
public static class JellyfinMediaSegmentMapper
{
    /// <summary>
    /// Maps a Jellyfin segment type string to a <see cref="MediaSegmentType"/>.
    /// Unrecognized values map to <see cref="MediaSegmentType.Unknown"/> so that
    /// future/unsupported types are ignored rather than skipped by mistake.
    /// </summary>
    public static MediaSegmentType ParseType(string jellyfinType) =>
        jellyfinType?.Trim().ToLowerInvariant() switch
        {
            "intro" => MediaSegmentType.Intro,
            "outro" => MediaSegmentType.Outro,
            "credits" => MediaSegmentType.Outro,
            "recap" => MediaSegmentType.Recap,
            "preview" => MediaSegmentType.Preview,
            "commercial" => MediaSegmentType.Commercial,
            _ => MediaSegmentType.Unknown
        };

    /// <summary>
    /// Builds a <see cref="MediaSegment"/> from Jellyfin tick offsets. Negative ticks
    /// are clamped to zero; the planner is responsible for any further validation.
    /// </summary>
    public static MediaSegment FromTicks(string jellyfinType, long startTicks, long endTicks)
    {
        TimeSpan start = TimeSpan.FromTicks(startTicks < 0 ? 0 : startTicks);
        TimeSpan end = TimeSpan.FromTicks(endTicks < 0 ? 0 : endTicks);
        return new MediaSegment(ParseType(jellyfinType), start, end);
    }
}
