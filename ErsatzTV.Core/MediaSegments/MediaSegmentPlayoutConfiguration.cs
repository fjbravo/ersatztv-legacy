using System.Text.Json.Serialization;

namespace ErsatzTV.Core.MediaSegments;

public class MediaSegmentPlayoutConfiguration
{
    [JsonPropertyName("showPolicies")]
    public List<ShowMediaSegmentSkipPolicy> ShowPolicies { get; set; } = [];

    [JsonPropertyName("itemSegments")]
    public List<MediaItemMediaSegmentCacheEntry> ItemSegments { get; set; } = [];
}

public class ShowMediaSegmentSkipPolicy
{
    public int ShowId { get; set; }
    public bool UseJellyfinMediaSegments { get; set; }
    public bool TrimIntros { get; set; }
    public bool SkipOutros { get; set; }
    public bool SkipRecaps { get; set; }
    public bool SkipPreviews { get; set; }
    public bool SkipCommercials { get; set; }
    public bool PreserveColdOpen { get; set; } = true;
    public int MinIntroDurationSeconds { get; set; } = 10;
    public int MaxIntroDurationSeconds { get; set; } = 180;
    public int MinOutroDurationSeconds { get; set; } = 10;
    public int MaxOutroDurationSeconds { get; set; } = 600;
    public double MinOutroStartPercent { get; set; } = 0.5;
    public int MinimumColdOpenSeconds { get; set; } = 10;

    public MediaSegmentSkipPolicy ToSkipPolicy() => new()
    {
        Enabled = UseJellyfinMediaSegments,
        SkipIntro = TrimIntros,
        SkipOutro = SkipOutros,
        SkipRecap = SkipRecaps,
        SkipPreview = SkipPreviews,
        SkipCommercial = SkipCommercials,
        PreserveColdOpen = PreserveColdOpen,
        MinIntroDuration = TimeSpan.FromSeconds(MinIntroDurationSeconds),
        MaxIntroDuration = TimeSpan.FromSeconds(MaxIntroDurationSeconds),
        MinOutroDuration = TimeSpan.FromSeconds(MinOutroDurationSeconds),
        MaxOutroDuration = TimeSpan.FromSeconds(MaxOutroDurationSeconds),
        MinOutroStartPercent = MinOutroStartPercent,
        MinimumColdOpen = TimeSpan.FromSeconds(MinimumColdOpenSeconds)
    };
}

public class MediaItemMediaSegmentCacheEntry
{
    public int MediaItemId { get; set; }
    public string JellyfinItemId { get; set; }
    public MediaSegmentType Type { get; set; }
    public long StartTicks { get; set; }
    public long EndTicks { get; set; }

    public MediaSegment ToMediaSegment() => new(Type, TimeSpan.FromTicks(StartTicks), TimeSpan.FromTicks(EndTicks));
}
