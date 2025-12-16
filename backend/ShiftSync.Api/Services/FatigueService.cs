using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Data;
using ShiftSync.Api.Models;

namespace ShiftSync.Api.Services
{
    public class FatigueService
    {
        private readonly AppDbContext _context;

        public FatigueService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Calculates and returns the fatigue score breakdown for a driver.
        /// FatigueScore = 100 * (0.40*AvgH3_norm + 0.25*OT7_norm + 0.20*Consecutive_norm + 0.10*RestGap_norm + 0.05*HeavyStops_norm)
        /// </summary>
        public async Task<FatigueBreakdown> CalculateFatigueScore(int driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                throw new Exception("Driver not found");

            var today = DateTime.UtcNow.Date;
            var threeDaysAgo = today.AddDays(-3);
            var sevenDaysAgo = today.AddDays(-7);

            // 1. Last 3-day average hours
            var last3DaysAttendance = await _context.Attendances
                .Where(a => a.DriverId == driverId && a.Date >= threeDaysAgo && a.Date < today)
                .ToListAsync();

            decimal avgHoursLast3Days = last3DaysAttendance.Any()
                ? last3DaysAttendance.Average(a => a.TotalHours)
                : 0;

            // Normalize: 10 hours = max fatigue (1.0)
            decimal avgH3Norm = Math.Min(1m, avgHoursLast3Days / 10m);

            // 2. Overtime days in last 7 days (total_hours > 8)
            var last7DaysAttendance = await _context.Attendances
                .Where(a => a.DriverId == driverId && a.Date >= sevenDaysAgo && a.Date < today)
                .ToListAsync();

            int overtimeDaysLast7 = last7DaysAttendance.Count(a => a.TotalHours > 8);

            // Normalize: 5 OT days = max fatigue (1.0)
            decimal ot7Norm = Math.Min(1m, overtimeDaysLast7 / 5m);

            // 3. Consecutive days worked (from driver record)
            int consecutiveDays = driver.ConsecutiveDays;

            // Normalize: 6 consecutive days = max fatigue (1.0)
            decimal consecutiveNorm = Math.Min(1m, consecutiveDays / 6m);

            // 4. Rest gap - hours since last checkout
            decimal restGapHours = 24; // Default to 24 hours if no recent checkout
            var lastCheckout = await _context.Attendances
                .Where(a => a.DriverId == driverId && a.CheckOutTime != null)
                .OrderByDescending(a => a.CheckOutTime)
                .FirstOrDefaultAsync();

            if (lastCheckout?.CheckOutTime != null)
            {
                restGapHours = (decimal)(DateTime.UtcNow - lastCheckout.CheckOutTime.Value).TotalHours;
            }

            // Normalize: 12+ hours rest = 0 fatigue, 0 hours rest = max fatigue (1.0)
            decimal restGapNorm = Math.Max(0m, 1m - (restGapHours / 12m));

            // 5. Heavy stop density - average stops from last 3 days assignments
            var last3DaysAssignments = await _context.ShiftAssignments
                .Include(s => s.Load)
                .Where(s => s.DriverId == driverId && s.AssignedDate >= threeDaysAgo && s.AssignedDate < today)
                .ToListAsync();

            decimal avgStopsLast3Days = last3DaysAssignments.Any() && last3DaysAssignments.Any(a => a.Load != null)
                ? (decimal)last3DaysAssignments.Where(a => a.Load != null).Average(a => a.Load!.Stops)
                : 0;

            // Normalize: 50 stops = max fatigue (1.0)
            decimal heavyStopsNorm = Math.Min(1m, avgStopsLast3Days / 50m);

            // Calculate final fatigue score (0-100)
            decimal fatigueScore = Math.Round(100m * (
                0.40m * avgH3Norm +
                0.25m * ot7Norm +
                0.20m * consecutiveNorm +
                0.10m * restGapNorm +
                0.05m * heavyStopsNorm
            ), 2);

            return new FatigueBreakdown
            {
                DriverId = driverId,
                DriverName = driver.Name,
                FatigueScore = fatigueScore,
                AvgHoursLast3Days = Math.Round(avgHoursLast3Days, 2),
                AvgH3Normalized = Math.Round(avgH3Norm, 4),
                OvertimeDaysLast7 = overtimeDaysLast7,
                OT7Normalized = Math.Round(ot7Norm, 4),
                ConsecutiveDays = consecutiveDays,
                ConsecutiveNormalized = Math.Round(consecutiveNorm, 4),
                RestGapHours = Math.Round(restGapHours, 2),
                RestGapNormalized = Math.Round(restGapNorm, 4),
                AvgStopsLast3Days = Math.Round(avgStopsLast3Days, 2),
                HeavyStopsNormalized = Math.Round(heavyStopsNorm, 4),
                CalculatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Updates a single driver's fatigue score in the database.
        /// </summary>
        public async Task<decimal> UpdateDriverFatigueScore(int driverId)
        {
            var breakdown = await CalculateFatigueScore(driverId);

            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver != null)
            {
                driver.FatigueScore = breakdown.FatigueScore;
                driver.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return breakdown.FatigueScore;
        }

        /// <summary>
        /// Updates fatigue scores for all active drivers.
        /// </summary>
        public async Task<List<FatigueUpdateResult>> UpdateAllDriverFatigueScores()
        {
            var results = new List<FatigueUpdateResult>();
            var activeDrivers = await _context.Drivers
                .Where(d => d.Status == "ACTIVE")
                .ToListAsync();

            foreach (var driver in activeDrivers)
            {
                try
                {
                    var breakdown = await CalculateFatigueScore(driver.DriverId);
                    driver.FatigueScore = breakdown.FatigueScore;
                    driver.UpdatedAt = DateTime.UtcNow;

                    results.Add(new FatigueUpdateResult
                    {
                        DriverId = driver.DriverId,
                        DriverName = driver.Name,
                        NewFatigueScore = breakdown.FatigueScore,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new FatigueUpdateResult
                    {
                        DriverId = driver.DriverId,
                        DriverName = driver.Name,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            await _context.SaveChangesAsync();
            return results;
        }

        /// <summary>
        /// Updates consecutive days for a driver after check-in.
        /// Call this method when a driver checks in.
        /// </summary>
        public async Task UpdateConsecutiveDays(int driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null) return;

            // Use UTC date range for PostgreSQL compatibility
            var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var yesterdayStart = todayStart.AddDays(-1);
            var yesterdayEnd = todayStart;

            // Check if driver worked yesterday
            var workedYesterday = await _context.Attendances
                .AnyAsync(a => a.DriverId == driverId &&
                              a.Date >= yesterdayStart && a.Date < yesterdayEnd &&
                              !a.IsAbsent);

            if (workedYesterday)
            {
                driver.ConsecutiveDays += 1;
            }
            else
            {
                driver.ConsecutiveDays = 1; // Reset to 1 (today)
            }

            driver.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets fatigue summary for all drivers (for admin dashboard).
        /// </summary>
        public async Task<FatigueSummary> GetFatigueSummary()
        {
            var drivers = await _context.Drivers
                .Where(d => d.Status == "ACTIVE")
                .ToListAsync();

            return new FatigueSummary
            {
                TotalActiveDrivers = drivers.Count,
                LowFatigueCount = drivers.Count(d => d.FatigueScore <= 40),
                MediumFatigueCount = drivers.Count(d => d.FatigueScore > 40 && d.FatigueScore <= 70),
                HighFatigueCount = drivers.Count(d => d.FatigueScore > 70),
                DriversNeedingRest = drivers.Count(d => d.FatigueScore > 85),
                AverageFatigueScore = drivers.Any() ? Math.Round(drivers.Average(d => d.FatigueScore), 2) : 0
            };
        }
    }

    // Supporting classes
    public class FatigueBreakdown
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public decimal FatigueScore { get; set; }
        public decimal AvgHoursLast3Days { get; set; }
        public decimal AvgH3Normalized { get; set; }
        public int OvertimeDaysLast7 { get; set; }
        public decimal OT7Normalized { get; set; }
        public int ConsecutiveDays { get; set; }
        public decimal ConsecutiveNormalized { get; set; }
        public decimal RestGapHours { get; set; }
        public decimal RestGapNormalized { get; set; }
        public decimal AvgStopsLast3Days { get; set; }
        public decimal HeavyStopsNormalized { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public class FatigueUpdateResult
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public decimal NewFatigueScore { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class FatigueSummary
    {
        public int TotalActiveDrivers { get; set; }
        public int LowFatigueCount { get; set; }
        public int MediumFatigueCount { get; set; }
        public int HighFatigueCount { get; set; }
        public int DriversNeedingRest { get; set; }
        public decimal AverageFatigueScore { get; set; }
    }
}
