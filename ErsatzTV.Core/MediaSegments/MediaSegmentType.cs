namespace ErsatzTV.Core.MediaSegments;

/// <summary>
/// Segment types using the Jellyfin Intro Skipper / Media Segments vocabulary.
/// "Outro" covers what some plugins label "Credits".
/// </summary>
public enum MediaSegmentType
{
    Unknown = 0,
    Intro = 1,
    Outro = 2,
    Recap = 3,
    Preview = 4,
    Commercial = 5
}
