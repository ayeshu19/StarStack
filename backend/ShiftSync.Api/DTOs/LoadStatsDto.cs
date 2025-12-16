namespace ShiftSync.Api.DTOs
{
    public class LoadStatsDto
    {
        public int TotalLoads { get; set; }
        public int PendingLoads { get; set; }
        public int AssignedLoads { get; set; }
        public int CompletedLoads { get; set; }
        public int HighPriorityPending { get; set; }
    }
}
