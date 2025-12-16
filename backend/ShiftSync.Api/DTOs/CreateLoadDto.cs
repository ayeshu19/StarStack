namespace ShiftSync.Api.DTOs
{
    public class CreateLoadDto
    {
        public string Region { get; set; } = string.Empty;
        public int Stops { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal EstimatedDistance { get; set; }
        public string Priority { get; set; } = "MEDIUM";
    }
}
