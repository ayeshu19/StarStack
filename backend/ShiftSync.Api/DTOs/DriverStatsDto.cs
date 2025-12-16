namespace ShiftSync.Api.DTOs
{
    public class DriverStatsDto
    {
        public int TotalDrivers { get; set; }
        public int ActiveDrivers { get; set; }
        public int InactiveDrivers { get; set; }
        public int HighFatigueDrivers { get; set; }
        public Dictionary<string, int> DriversByRegion { get; set; } = new();
    }
}