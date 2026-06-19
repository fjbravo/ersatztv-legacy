using ErsatzTV.Core.MediaSegments;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.MediaSegments;

[TestFixture]
public class MediaSegmentRangePlannerTests
{
    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(45);

    [Test]
    public void PlanRanges_DisabledPolicy_PlaysFullItem()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(MediaSegmentType.Intro, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2))],
            MediaSegmentSkipPolicy.Disabled);

        ranges.ShouldBe([new PlaybackRange(TimeSpan.Zero, Duration)]);
    }

    [Test]
    public void PlanRanges_NoSegments_PlaysFullItem()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [],
            new MediaSegmentSkipPolicy { Enabled = true, SkipIntro = true });

        ranges.ShouldBe([new PlaybackRange(TimeSpan.Zero, Duration)]);
    }

    [Test]
    public void PlanRanges_InvalidActiveSegment_PlaysFullItem()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(MediaSegmentType.Intro, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1))],
            new MediaSegmentSkipPolicy { Enabled = true, SkipIntro = true });

        ranges.ShouldBe([new PlaybackRange(TimeSpan.Zero, Duration)]);
    }

    [Test]
    public void PlanRanges_PreservesColdOpenBySplittingAroundIntro()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(MediaSegmentType.Intro, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(4))],
            new MediaSegmentSkipPolicy { Enabled = true, SkipIntro = true, PreserveColdOpen = true });

        ranges.ShouldBe([
            new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(3)),
            new PlaybackRange(TimeSpan.FromMinutes(4), Duration)
        ]);
    }

    [Test]
    public void PlanRanges_CanSkipFromBeginningWhenColdOpenIsNotPreserved()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(MediaSegmentType.Intro, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(4))],
            new MediaSegmentSkipPolicy { Enabled = true, SkipIntro = true, PreserveColdOpen = false });

        ranges.ShouldBe([new PlaybackRange(TimeSpan.FromMinutes(4), Duration)]);
    }

    [Test]
    public void PlanRanges_OutroEndsPlaybackAtCreditsStart()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(MediaSegmentType.Outro, TimeSpan.FromMinutes(42), Duration)],
            new MediaSegmentSkipPolicy { Enabled = true, SkipOutro = true });

        ranges.ShouldBe([new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(42))]);
    }

    [Test]
    public void PlanRanges_IntroAndOutro_ProducesDeterministicEffectiveRanges()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [
                new MediaSegment(MediaSegmentType.Intro, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(4)),
                new MediaSegment(MediaSegmentType.Outro, TimeSpan.FromMinutes(42), Duration)
            ],
            new MediaSegmentSkipPolicy
            {
                Enabled = true,
                SkipIntro = true,
                SkipOutro = true,
                PreserveColdOpen = true
            });

        ranges.ShouldBe([
            new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(3)),
            new PlaybackRange(TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(42))
        ]);
    }

    [Test]
    public void PlanRanges_UnsupportedSegmentToggleOff_IsIgnored()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(MediaSegmentType.Preview, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(6))],
            new MediaSegmentSkipPolicy { Enabled = true, SkipIntro = true });

        ranges.ShouldBe([new PlaybackRange(TimeSpan.Zero, Duration)]);
    }

    [TestCase(MediaSegmentType.Recap)]
    [TestCase(MediaSegmentType.Preview)]
    [TestCase(MediaSegmentType.Commercial)]
    public void PlanRanges_SupportsJellyfinIntroSkipperSegmentVocabulary(MediaSegmentType type)
    {
        var policy = new MediaSegmentSkipPolicy { Enabled = true };
        switch (type)
        {
            case MediaSegmentType.Recap:
                policy.SkipRecap = true;
                break;
            case MediaSegmentType.Preview:
                policy.SkipPreview = true;
                break;
            case MediaSegmentType.Commercial:
                policy.SkipCommercial = true;
                break;
        }

        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(type, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(6))],
            policy);

        ranges.ShouldBe([
            new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(5)),
            new PlaybackRange(TimeSpan.FromMinutes(6), Duration)
        ]);
    }

    [Test]
    public void PlanRanges_SuspiciousEarlyOutro_PlaysFullItem()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(MediaSegmentType.Outro, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(11))],
            new MediaSegmentSkipPolicy { Enabled = true, SkipOutro = true });

        ranges.ShouldBe([new PlaybackRange(TimeSpan.Zero, Duration)]);
    }

    [Test]
    public void PlanRanges_IntroOutsideDurationThreshold_PlaysFullItem()
    {
        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            Duration,
            [new MediaSegment(MediaSegmentType.Intro, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(2)))],
            new MediaSegmentSkipPolicy { Enabled = true, SkipIntro = true });

        ranges.ShouldBe([new PlaybackRange(TimeSpan.Zero, Duration)]);
    }
}
