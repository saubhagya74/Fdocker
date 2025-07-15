using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace app.Models
{
    public class FriendRequestEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Prevents EF from auto-generating if you use Snowflake IDs
        public long RequestId { get; set; }
        public long RequesterId { get; set; }
        public long RequestToId{ get; set; }
        public DateTime RequestTime { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
    }
}
