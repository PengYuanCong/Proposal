namespace Proposal.Models
{
    public class VideoItem
    {
        public string VideoId { get; set; }    // YouTube 的影片 ID
        public string Title { get; set; }      // 影片標題
        public string ThumbnailUrl { get; set; } // 預覽圖網址
        public string Description { get; set; } // 影片描述 (選填)
    }
}