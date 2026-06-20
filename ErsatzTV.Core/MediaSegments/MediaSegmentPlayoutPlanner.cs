using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;

namespace ErsatzTV.Core.MediaSegments;

public static class MediaSegmentPlayoutPlanner
{
    public static IReadOnlyList<PlaybackRange> GetPlaybackRanges(
        MediaItem mediaItem,
        IReadOnlyDictionary<int, IReadOnlyList<PlaybackRange>> playbackRangesByMediaItemId)
    {
        if (playbackRangesByMediaItemId is not null &&
            playbackRangesByMediaItemId.TryGetValue(mediaItem.Id, out IReadOnlyList<PlaybackRange> ranges) &&
            ranges.Count > 0)
        {
            return ranges;
        }

        TimeSpan duration = mediaItem.GetDurationForPlayout();
        return [new PlaybackRange(TimeSpan.Zero, duration)];
    }

    public static TimeSpan GetEffectiveDuration(
        MediaItem mediaItem,
        IReadOnlyDictionary<int, IReadOnlyList<PlaybackRange>> playbackRangesByMediaItemId) =>
        TimeSpan.FromTicks(GetPlaybackRanges(mediaItem, playbackRangesByMediaItemId).Sum(r => r.Duration.Ticks));

    public static IReadOnlyList<PlaybackRange> TruncateRanges(IReadOnlyList<PlaybackRange> ranges, TimeSpan duration)
    {
        var result = new List<PlaybackRange>();
        TimeSpan remaining = duration;

        foreach (PlaybackRange range in ranges)
        {
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            TimeSpan rangeDuration = range.Duration <= remaining ? range.Duration : remaining;
            result.Add(new PlaybackRange(range.InPoint, range.InPoint + rangeDuration));
            remaining -= rangeDuration;
        }

        return result;
    }

    public static List<PlayoutItem> ApplyRanges(PlayoutItem template, IReadOnlyList<PlaybackRange> ranges)
    {
        var result = new List<PlayoutItem>();
        DateTime currentStart = template.Start;

        foreach (PlaybackRange range in ranges)
        {
            var item = new PlayoutItem
            {
                MediaItemId = template.MediaItemId,
                MediaItem = template.MediaItem,
                Start = currentStart,
                Finish = currentStart + range.Duration,
                GuideStart = template.GuideStart,
                GuideFinish = template.GuideFinish,
                CustomTitle = template.CustomTitle,
                GuideGroup = template.GuideGroup,
                FillerKind = template.FillerKind,
                PlayoutId = template.PlayoutId,
                Playout = template.Playout,
                InPoint = range.InPoint,
                OutPoint = range.OutPoint,
                ChapterTitle = template.ChapterTitle,
                DisableWatermarks = template.DisableWatermarks,
                PreferredAudioLanguageCode = template.PreferredAudioLanguageCode,
                PreferredAudioTitle = template.PreferredAudioTitle,
                PreferredSubtitleLanguageCode = template.PreferredSubtitleLanguageCode,
                SubtitleMode = template.SubtitleMode,
                BlockKey = template.BlockKey,
                CollectionKey = template.CollectionKey,
                CollectionEtag = template.CollectionEtag,
                SchedulingContext = template.SchedulingContext,
                PlayoutItemWatermarks = [],
                PlayoutItemGraphicsElements = []
            };

            foreach (PlayoutItemWatermark watermark in template.PlayoutItemWatermarks ?? [])
            {
                item.PlayoutItemWatermarks.Add(
                    new PlayoutItemWatermark
                    {
                        PlayoutItem = item,
                        WatermarkId = watermark.WatermarkId
                    });
            }

            foreach (PlayoutItemGraphicsElement graphicsElement in template.PlayoutItemGraphicsElements ?? [])
            {
                item.PlayoutItemGraphicsElements.Add(
                    new PlayoutItemGraphicsElement
                    {
                        PlayoutItem = item,
                        GraphicsElementId = graphicsElement.GraphicsElementId,
                        Variables = graphicsElement.Variables
                    });
            }

            result.Add(item);
            currentStart += range.Duration;
        }

        return result;
    }
}
