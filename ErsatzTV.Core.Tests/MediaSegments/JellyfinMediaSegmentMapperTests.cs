using ErsatzTV.Core.MediaSegments;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.MediaSegments;

[TestFixture]
public class JellyfinMediaSegmentMapperTests
{
    [TestCase("Intro", MediaSegmentType.Intro)]
    [TestCase("intro", MediaSegmentType.Intro)]
    [TestCase("Outro", MediaSegmentType.Outro)]
    [TestCase("Credits", MediaSegmentType.Outro)]
    [TestCase("Recap", MediaSegmentType.Recap)]
    [TestCase("Preview", MediaSegmentType.Preview)]
    [TestCase("Commercial", MediaSegmentType.Commercial)]
    public void ParseType_MapsKnownJellyfinVocabulary(string input, MediaSegmentType expected)
    {
        JellyfinMediaSegmentMapper.ParseType(input).ShouldBe(expected);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    [TestCase("SomethingNew")]
    public void ParseType_UnknownOrEmpty_MapsToUnknown(string input)
    {
        JellyfinMediaSegmentMapper.ParseType(input).ShouldBe(MediaSegmentType.Unknown);
    }

    [Test]
    public void FromTicks_ConvertsTicksToTimeSpan()
    {
        TimeSpan start = TimeSpan.FromSeconds(30);
        TimeSpan end = TimeSpan.FromSeconds(90);

        MediaSegment segment = JellyfinMediaSegmentMapper.FromTicks("Intro", start.Ticks, end.Ticks);

        segment.Type.ShouldBe(MediaSegmentType.Intro);
        segment.Start.ShouldBe(start);
        segment.End.ShouldBe(end);
    }

    [Test]
    public void FromTicks_NegativeTicks_AreClampedToZero()
    {
        MediaSegment segment = JellyfinMediaSegmentMapper.FromTicks("Outro", -100, -50);

        segment.Start.ShouldBe(TimeSpan.Zero);
        segment.End.ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void FromTicks_FeedsPlannerToProduceSkipRanges()
    {
        TimeSpan duration = TimeSpan.FromMinutes(30);

        MediaSegment intro = JellyfinMediaSegmentMapper.FromTicks(
            "Intro",
            TimeSpan.FromMinutes(1).Ticks,
            TimeSpan.FromMinutes(2).Ticks);

        IReadOnlyList<PlaybackRange> ranges = MediaSegmentRangePlanner.PlanRanges(
            duration,
            [intro],
            new MediaSegmentSkipPolicy { Enabled = true, SkipIntro = true, PreserveColdOpen = true });

        ranges.ShouldBe([
            new PlaybackRange(TimeSpan.Zero, TimeSpan.FromMinutes(1)),
            new PlaybackRange(TimeSpan.FromMinutes(2), duration)
        ]);
    }
}
