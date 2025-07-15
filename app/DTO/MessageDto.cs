namespace app.DTO
{
    public class MessageDto
    {
        public string SenderId { get; set; }=default!;
        public string ReceiverId { get; set; } = default!;
        public string Content { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
    }
}
