namespace app.DTO
{
    public class RefreshTokenRequest
    {
        public class RefreshTokenRequestDto
        {
            public long UserId { get; set; }
            public required string RefreshToken { get; set; } = string.Empty;
        }
    }
}
