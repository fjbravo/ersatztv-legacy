using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.MediaSegments;

namespace ErsatzTV.Core.Scheduling;

public record PlayoutBuilderState(
    int PlayoutId,
    IScheduleItemsEnumerator ScheduleItemsEnumerator,
    Option<int> MultipleRemaining,
    Option<DateTimeOffset> DurationFinish,
    bool InFlood,
    bool InDurationFiller,
    int NextGuideGroup,
    DateTimeOffset CurrentTime,
    IReadOnlyDictionary<int, IReadOnlyList<PlaybackRange>> MediaSegmentPlaybackRanges = null)
{
    public IReadOnlyDictionary<int, IReadOnlyList<PlaybackRange>> MediaSegmentPlaybackRanges { get; } =
        MediaSegmentPlaybackRanges ?? new Dictionary<int, IReadOnlyList<PlaybackRange>>();

    public int IncrementGuideGroup => (NextGuideGroup + 1) % 10000;
    public int DecrementGuideGroup => (NextGuideGroup - 1) % 10000;
}
