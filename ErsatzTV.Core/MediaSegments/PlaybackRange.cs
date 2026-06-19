namespace ErsatzTV.Core.MediaSegments;

/// <summary>
/// A contiguous span of a media item that should be played, expressed as in/out
/// points into the source. Maps directly onto PlayoutItem InPoint/OutPoint.
/// </summary>
public record PlaybackRange(TimeSpan InPoint, TimeSpan OutPoint)
{
    public TimeSpan Duration => OutPoint - InPoint;
}
