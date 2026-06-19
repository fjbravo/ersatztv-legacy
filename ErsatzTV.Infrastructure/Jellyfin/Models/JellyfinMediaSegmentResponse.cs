namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinMediaSegmentResponse
{
    public string Id { get; set; }
    public string ItemId { get; set; }
    public string Type { get; set; }
    public long StartTicks { get; set; }
    public long EndTicks { get; set; }
}
