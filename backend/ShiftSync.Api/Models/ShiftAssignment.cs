using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShiftSync.Api.Models
{
    [Table("shift_assignments")]
    public class ShiftAssignment
    {
        [Key]
        [Column("assignment_id")]
        public int AssignmentId { get; set; }

        [Column("driver_id")]
        public int DriverId { get; set; }

        [ForeignKey("DriverId")]
        public Driver? Driver { get; set; }

        [Column("load_ref")]
        public string LoadRef { get; set; } = string.Empty;

        [Column("assigned_date")]
        public DateTime AssignedDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "ASSIGNED";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
