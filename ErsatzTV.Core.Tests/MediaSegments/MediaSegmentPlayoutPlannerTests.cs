using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.MediaSegments;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.MediaSegments;

[TestFixture]
public class MediaSegmentPlayoutPlannerTests
{
    [Test]
    public void GetPlaybackRanges_ConfiguredRanges_ReturnsConfiguredRanges()
    {
        MediaItem mediaItem = TestMovie(1, TimeSpan.FromMinutes(45));
        IReadOnlyList<PlaybackRange> configuredRanges = [
            new(TimeSpan.Zero, TimeSpan.FromMinutes(3)),
            new(TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(45))
        ];

        IReadOnlyList<PlaybackRange> ranges = MediaSegmentPlayoutPlanner.GetPlaybackRanges(
            mediaItem,
            new Dictionary<int, IReadOnlyList<PlaybackRange>> { { 1, configuredRanges } });

        ranges.ShouldBe(configuredRanges);
    }

    [Test]
    public void GetPlaybackRanges_NoConfiguredRanges_ReturnsFullMediaDuration()
    {
        MediaItem mediaItem = TestMovie(1, TimeSpan.FromMinutes(45));

        IReadOnlyList<PlaybackRange> ranges = MediaSegmentPlayoutPlanner.GetPlaybackRanges(
            mediaItem,
            new Dictionary<int, IReadOnlyList<PlaybackRange>>());

        ranges.ShouldBe([new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(45))]);
    }

    [Test]
    public void GetEffectiveDuration_ConfiguredRanges_ReturnsSumOfRangeDurations()
    {
        MediaItem mediaItem = TestMovie(1, TimeSpan.FromMinutes(45));

        TimeSpan duration = MediaSegmentPlayoutPlanner.GetEffectiveDuration(
            mediaItem,
            new Dictionary<int, IReadOnlyList<PlaybackRange>>
            {
                {
                    1,
                    [
                        new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(3)),
                        new PlaybackRange(TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(42))
                    ]
                }
            });

        duration.ShouldBe(TimeSpan.FromMinutes(41));
    }

    [Test]
    public void TruncateRanges_PreservesSkippedSourceOffsetsWhenTrimFallsInsideLaterRange()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentPlayoutPlanner.TruncateRanges(
            [
                new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(10)),
                new PlaybackRange(TimeSpan.FromMinutes(20), TimeSpan.FromHours(1))
            ],
            TimeSpan.FromMinutes(25));

        ranges.ShouldBe([
            new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(10)),
            new PlaybackRange(TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(35))
        ]);
    }

    [Test]
    public void ApplyRanges_MultipleRanges_SplitsSequentialItemsAndPreservesMetadata()
    {
        var start = new DateTime(2020, 10, 18, 12, 0, 0, DateTimeKind.Utc);
        var template = new PlayoutItem
        {
            PlayoutId = 11,
            MediaItemId = 1,
            MediaItem = TestMovie(1, TimeSpan.FromMinutes(45)),
            Start = start,
            Finish = start.AddMinutes(44),
            GuideStart = start,
            GuideFinish = start.AddMinutes(44),
            CustomTitle = "Custom",
            GuideGroup = 7,
            FillerKind = FillerKind.None,
            PreferredAudioLanguageCode = "eng",
            PreferredAudioTitle = "Stereo",
            PreferredSubtitleLanguageCode = "spa",
            SubtitleMode = ChannelSubtitleMode.Forced,
            BlockKey = "block",
            CollectionKey = "collection",
            CollectionEtag = "etag",
            SchedulingContext = "context",
            PlayoutItemWatermarks = [new PlayoutItemWatermark { WatermarkId = 123 }],
            PlayoutItemGraphicsElements = [new PlayoutItemGraphicsElement { GraphicsElementId = 456, Variables = "{}" }]
        };

        List<PlayoutItem> result = MediaSegmentPlayoutPlanner.ApplyRanges(
            template,
            [
                new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(3)),
                new PlaybackRange(TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(42))
            ]);

        result.Count.ShouldBe(2);

        result[0].Start.ShouldBe(start);
        result[0].Finish.ShouldBe(start.AddMinutes(3));
        result[0].InPoint.ShouldBe(TimeSpan.Zero);
        result[0].OutPoint.ShouldBe(TimeSpan.FromMinutes(3));

        result[1].Start.ShouldBe(start.AddMinutes(3));
        result[1].Finish.ShouldBe(start.AddMinutes(41));
        result[1].InPoint.ShouldBe(TimeSpan.FromMinutes(4));
        result[1].OutPoint.ShouldBe(TimeSpan.FromMinutes(42));

        result.ShouldAllBe(item => item.MediaItemId == template.MediaItemId);
        result.ShouldAllBe(item => item.PlayoutId == template.PlayoutId);
        result.ShouldAllBe(item => item.GuideGroup == template.GuideGroup);
        result.ShouldAllBe(item => item.CustomTitle == template.CustomTitle);
        result.ShouldAllBe(item => item.SchedulingContext == template.SchedulingContext);
        result.ShouldAllBe(item => item.PlayoutItemWatermarks.Count == 1);
        result.ShouldAllBe(item => item.PlayoutItemGraphicsElements.Count == 1);
        result.ShouldAllBe(item => item.PlayoutItemWatermarks[0].PlayoutItem == item);
        result.ShouldAllBe(item => item.PlayoutItemGraphicsElements[0].PlayoutItem == item);
    }

    private static Movie TestMovie(int id, TimeSpan duration) => new()
    {
        Id = id,
        MediaVersions = [new MediaVersion { Duration = duration }]
    };
}
