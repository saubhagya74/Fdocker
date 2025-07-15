using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace app.Models
{
    public class FriendEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Prevents EF from auto-generating if you use Snowflake IDs
        public long FriendId{ get; set; }
        public long FUserId1 { get; set; }
        public long FUserId2 { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime FriendSince { get; set; }
    }
}
