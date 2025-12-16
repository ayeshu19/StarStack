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

        [Column("load_id")]
        public int? LoadId { get; set; }

        [ForeignKey("LoadId")]
        public Load? Load { get; set; }

        [Column("load_ref")]
        public string LoadRef { get; set; } = string.Empty;

        [Column("assigned_date")]
        public DateTime AssignedDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "ASSIGNED";

        [Column("suitability_score")]
        public decimal SuitabilityScore { get; set; } = 0;

        [Column("overload_score")]
        public decimal OverloadScore { get; set; } = 0;

        [Column("is_override")]
        public bool IsOverride { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
