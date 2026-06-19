namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinMediaSegmentsResponse
{
    public List<JellyfinMediaSegmentResponse> Items { get; set; }
    public int TotalRecordCount { get; set; }
}
