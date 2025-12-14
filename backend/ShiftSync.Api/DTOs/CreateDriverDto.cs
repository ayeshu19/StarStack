namespace ShiftSync.Api.DTOs
{
    public class CreateDriverDto
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Email { get; set; }
        public string Region { get; set; } = "";
        public string? Password { get; set; }
        public string VehicleType { get; set; } = "truck";
    public string WeeklyOff { get; set; } = "SUNDAY";
    public string Status { get; set; } = "ACTIVE";
    public decimal? FatigueScore { get; set; } = 0;
    }
}
