namespace ShiftSync.Api.DTOs
{
    public class UpdateLoadDto
    {
        public string Region { get; set; } = string.Empty;
        public int Stops { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal EstimatedDistance { get; set; }
        public string Priority { get; set; } = "MEDIUM";
        public string Status { get; set; } = "PENDING";
        public int? AssignedDriverId { get; set; } // optional if you allow manual assign here
    }
}

