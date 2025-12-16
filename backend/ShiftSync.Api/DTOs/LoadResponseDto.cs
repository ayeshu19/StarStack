namespace ShiftSync.Api.DTOs
{
    public class LoadResponseDto
    {
        public int LoadId { get; set; }
        public string LoadRef { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int Stops { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal EstimatedDistance { get; set; }
        public string Priority { get; set; } = "MEDIUM";
        public string Status { get; set; } = "PENDING";
        public int? AssignedDriverId { get; set; }
        public string? AssignedDriverName { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
