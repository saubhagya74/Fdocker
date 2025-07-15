namespace app.DTO
{
    public class NotificationDto
    {
        public string RequesterId { get; set; } = default!;
        public string RequesterName { get; set; } = string.Empty;
        public string RequestToId { get; set; } = default!;
        public string RequestToName { get; set; } = string.Empty;
        public DateTime RequestTime { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
    }
}
