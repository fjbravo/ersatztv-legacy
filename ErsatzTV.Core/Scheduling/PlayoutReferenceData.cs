using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.MediaSegments;

namespace ErsatzTV.Core.Scheduling;

public record PlayoutReferenceData(
    Channel Channel,
    Option<Deco> Deco,
    List<PlayoutItem> ExistingItems,
    List<PlayoutTemplate> PlayoutTemplates,
    ProgramSchedule ProgramSchedule,
    List<ProgramScheduleAlternate> ProgramScheduleAlternates,
    List<PlayoutHistory> PlayoutHistory,
    TimeSpan MaxPlayoutOffset,
    IReadOnlyDictionary<int, IReadOnlyList<PlaybackRange>> MediaSegmentPlaybackRanges = null)
{
    public IReadOnlyDictionary<int, IReadOnlyList<PlaybackRange>> MediaSegmentPlaybackRanges { get; } =
        MediaSegmentPlaybackRanges ?? new Dictionary<int, IReadOnlyList<PlaybackRange>>();
}
