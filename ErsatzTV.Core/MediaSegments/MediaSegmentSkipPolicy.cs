namespace ErsatzTV.Core.MediaSegments;

/// <summary>
/// Per-show (or per-channel) configuration controlling which Jellyfin media
/// segment types are skipped during playback. Defaults to fully disabled so that
/// existing channels are unchanged unless a policy is explicitly enabled.
/// </summary>
public class MediaSegmentSkipPolicy
{
    /// <summary>Master switch. When false, no segments are skipped.</summary>
    public bool Enabled { get; set; }

    public bool SkipIntro { get; set; }
    public bool SkipOutro { get; set; }
    public bool SkipRecap { get; set; }
    public bool SkipPreview { get; set; }
    public bool SkipCommercial { get; set; }

    /// <summary>
    /// When true, content before an intro (a cold open) is preserved by splitting
    /// playback around the intro instead of skipping from the very start of the item.
    /// </summary>
    public bool PreserveColdOpen { get; set; }

    public TimeSpan MinIntroDuration { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan MaxIntroDuration { get; set; } = TimeSpan.FromSeconds(180);
    public TimeSpan MinOutroDuration { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan MaxOutroDuration { get; set; } = TimeSpan.FromSeconds(600);

    /// <summary>
    /// Credits/outros before this percentage of the item are treated as bad data.
    /// </summary>
    public double MinOutroStartPercent { get; set; } = 0.5;

    /// <summary>
    /// Minimum length of pre-intro content required before it is treated as a cold
    /// open worth preserving. Avoids producing tiny throwaway ranges.
    /// </summary>
    public TimeSpan MinimumColdOpen { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>A disabled policy that skips nothing.</summary>
    public static MediaSegmentSkipPolicy Disabled => new() { Enabled = false };

    public bool ShouldSkip(MediaSegmentType type) => Enabled && type switch
    {
        MediaSegmentType.Intro => SkipIntro,
        MediaSegmentType.Outro => SkipOutro,
        MediaSegmentType.Recap => SkipRecap,
        MediaSegmentType.Preview => SkipPreview,
        MediaSegmentType.Commercial => SkipCommercial,
        _ => false
    };

    /// <summary>True when the policy is enabled and skips at least one segment type.</summary>
    public bool HasAnySkip => Enabled &&
                              (SkipIntro || SkipOutro || SkipRecap || SkipPreview || SkipCommercial);
}
