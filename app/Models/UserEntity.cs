using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace app.Models
{
    public class UserEntity
    {
        [Key] // Makes UserId the primary key
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Prevents EF from auto-generating if you use Snowflake IDs
        public long UserId { get; set; }
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ConnectionId { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public short NumOfFriends { get; set; } = 0;
    }
}
