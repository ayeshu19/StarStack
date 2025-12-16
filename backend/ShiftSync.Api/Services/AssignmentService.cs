using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Data;
using ShiftSync.Api.Models;

namespace ShiftSync.Api.Services
{
    public class AssignmentService
    {
        private readonly AppDbContext _context;
        private readonly FatigueService _fatigueService;

        public AssignmentService(AppDbContext context, FatigueService fatigueService)
        {
            _context = context;
            _fatigueService = fatigueService;
        }

        #region Overload Prediction Engine

        /// <summary>
        /// Calculates overload score for a driver if they were to take on a new load.
        /// OverloadScore = 0.50*StopsNorm + 0.30*HoursNorm + 0.20*DistanceNorm
        /// </summary>
        public async Task<OverloadResult> CalculateOverloadScore(int driverId, int loadId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            var load = await _context.Loads.FindAsync(loadId);

            if (driver == null) throw new Exception("Driver not found");
            if (load == null) throw new Exception("Load not found");

            // Use UTC date range for PostgreSQL compatibility
            var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var todayEnd = todayStart.AddDays(1);

            // Get driver's current assignments for today
            var todayAssignments = await _context.ShiftAssignments
                .Include(s => s.Load)
                .Where(s => s.DriverId == driverId &&
                           s.AssignedDate >= todayStart && s.AssignedDate < todayEnd &&
                           s.Status != "COMPLETED")
                .ToListAsync();

            // Current workload
            int currentStops = todayAssignments.Sum(a => a.Load?.Stops ?? 0);
            decimal currentHours = todayAssignments.Sum(a => a.Load?.EstimatedHours ?? 0);
            decimal currentDistance = todayAssignments.Sum(a => a.Load?.EstimatedDistance ?? 0);

            // Projected workload (current + new load)
            int projectedStops = currentStops + load.Stops;
            decimal projectedHours = currentHours + load.EstimatedHours;
            decimal projectedDistance = currentDistance + load.EstimatedDistance;

            // Normalize (60 stops, 10 hours, 200km = max)
            decimal stopsNorm = Math.Min(1m, projectedStops / 60m);
            decimal hoursNorm = Math.Min(1m, projectedHours / 10m);
            decimal distanceNorm = Math.Min(1m, projectedDistance / 200m);

            // Calculate overload score
            decimal overloadScore = Math.Round(
                0.50m * stopsNorm +
                0.30m * hoursNorm +
                0.20m * distanceNorm
            , 4);

            // Determine status
            string status;
            if (overloadScore < 0.75m)
                status = "SAFE";
            else if (overloadScore < 0.90m)
                status = "WARNING";
            else
                status = "UNSAFE";

            return new OverloadResult
            {
                DriverId = driverId,
                DriverName = driver.Name,
                LoadId = loadId,
                OverloadScore = overloadScore,
                Status = status,
                CurrentStops = currentStops,
                CurrentHours = currentHours,
                CurrentDistance = currentDistance,
                ProjectedStops = projectedStops,
                ProjectedHours = projectedHours,
                ProjectedDistance = projectedDistance,
                StopsNormalized = Math.Round(stopsNorm, 4),
                HoursNormalized = Math.Round(hoursNorm, 4),
                DistanceNormalized = Math.Round(distanceNorm, 4)
            };
        }

        #endregion

        #region Suitability Score Engine

        /// <summary>
        /// Calculates suitability score for assigning a load to a driver.
        /// Suitability = 0.30*RegionScore + 0.25*WorkloadNorm + 0.25*FatigueNorm + 0.10*DistanceNorm + 0.10*RotationPenalty
        /// </summary>
        public async Task<SuitabilityResult> CalculateSuitabilityScore(int driverId, int loadId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            var load = await _context.Loads.FindAsync(loadId);

            if (driver == null) throw new Exception("Driver not found");
            if (load == null) throw new Exception("Load not found");

            // Use UTC date range for PostgreSQL compatibility
            var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var todayEnd = todayStart.AddDays(1);

            // 1. Region Score (100 if match, 50 if not)
            decimal regionScore = driver.Region.Equals(load.Region, StringComparison.OrdinalIgnoreCase) ? 100m : 50m;

            // 2. Workload Score (less work today = higher score)
            var todayAssignments = await _context.ShiftAssignments
                .Include(s => s.Load)
                .Where(s => s.DriverId == driverId &&
                           s.AssignedDate >= todayStart && s.AssignedDate < todayEnd &&
                           s.Status != "COMPLETED")
                .ToListAsync();

            decimal currentHours = todayAssignments.Sum(a => a.Load?.EstimatedHours ?? 0);
            decimal workloadNorm = 100m - (Math.Min(1m, currentHours / 10m) * 100m);

            // 3. Fatigue Score (lower fatigue = higher suitability)
            decimal fatigueNorm = 100m - driver.FatigueScore;

            // 4. Distance Score (less distance today = higher score)
            decimal currentDistance = todayAssignments.Sum(a => a.Load?.EstimatedDistance ?? 0);
            decimal distanceNorm = 100m - (Math.Min(1m, currentDistance / 200m) * 100m);

            // 5. Rotation Penalty (-20 if worked 5+ consecutive days)
            decimal rotationPenalty = driver.ConsecutiveDays >= 5 ? -20m : 0m;

            // Calculate final suitability score
            decimal suitabilityScore = Math.Round(
                0.30m * regionScore +
                0.25m * workloadNorm +
                0.25m * fatigueNorm +
                0.10m * distanceNorm +
                0.10m * (100m + rotationPenalty) // Convert penalty to 0-100 scale
            , 2);

            // Clamp to 0-100
            suitabilityScore = Math.Max(0m, Math.Min(100m, suitabilityScore));

            return new SuitabilityResult
            {
                DriverId = driverId,
                DriverName = driver.Name,
                LoadId = loadId,
                SuitabilityScore = suitabilityScore,
                RegionMatch = driver.Region.Equals(load.Region, StringComparison.OrdinalIgnoreCase),
                RegionScore = regionScore,
                WorkloadScore = Math.Round(workloadNorm, 2),
                FatigueScore = Math.Round(fatigueNorm, 2),
                DistanceScore = Math.Round(distanceNorm, 2),
                RotationPenalty = rotationPenalty,
                ConsecutiveDays = driver.ConsecutiveDays,
                CurrentFatigueScore = driver.FatigueScore
            };
        }

        #endregion

        #region Driver Eligibility Check

        /// <summary>
        /// Checks if a driver is eligible for assignment.
        /// </summary>
        public async Task<EligibilityResult> CheckDriverEligibility(int driverId, int loadId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                return new EligibilityResult { IsEligible = false, Reason = "Driver not found" };

            var reasons = new List<string>();
            bool isEligible = true;

            // 1. Check if driver is active
            if (driver.Status != "ACTIVE")
            {
                isEligible = false;
                reasons.Add("Driver is inactive");
            }

            // 2. Check if today is driver's weekly off
            var todayDayOfWeek = DateTime.UtcNow.DayOfWeek.ToString().ToUpper();
            if (driver.WeeklyOff.ToUpper() == todayDayOfWeek)
            {
                isEligible = false;
                reasons.Add($"Today is driver's weekly off ({driver.WeeklyOff})");
            }

            // 3. Check if fatigue score > 85 (forced rest)
            if (driver.FatigueScore > 85)
            {
                isEligible = false;
                reasons.Add($"Fatigue score too high ({driver.FatigueScore}/100) - driver needs rest");
            }

            // 4. Check if driver already has 3+ loads assigned today
            // Use UTC date range for PostgreSQL compatibility
            var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var todayEnd = todayStart.AddDays(1);
            var todayLoadCount = await _context.ShiftAssignments
                .CountAsync(s => s.DriverId == driverId &&
                                s.AssignedDate >= todayStart && s.AssignedDate < todayEnd &&
                                s.Status != "COMPLETED");

            if (todayLoadCount >= 3)
            {
                isEligible = false;
                reasons.Add($"Driver already has {todayLoadCount} active loads today (max 3)");
            }

            // 5. Check if current overload score > 0.90
            if (loadId > 0)
            {
                var overload = await CalculateOverloadScore(driverId, loadId);
                if (overload.OverloadScore > 0.90m)
                {
                    isEligible = false;
                    reasons.Add($"Overload score too high ({overload.OverloadScore:P0}) - unsafe");
                }
            }

            // 6. Check if driver has checked in today (using in-memory filter for PostgreSQL compatibility)
            var driverAttendances = await _context.Attendances
                .Where(a => a.DriverId == driverId)
                .ToListAsync();

            var checkedInToday = driverAttendances
                .Any(a => a.Date.Date == todayStart.Date && a.CheckInTime != null);

            if (!checkedInToday)
            {
                isEligible = false;
                reasons.Add("Driver has not checked in today");
            }

            return new EligibilityResult
            {
                DriverId = driverId,
                DriverName = driver.Name,
                IsEligible = isEligible,
                Reason = reasons.Any() ? string.Join("; ", reasons) : "Eligible for assignment"
            };
        }

        #endregion

        #region Auto Assignment Engine

        /// <summary>
        /// Gets recommended drivers for a load, ranked by suitability score.
        /// </summary>
        public async Task<AssignmentRecommendation> GetRecommendations(int loadId)
        {
            var load = await _context.Loads.FindAsync(loadId);
            if (load == null)
                throw new Exception("Load not found");

            if (load.Status != "PENDING")
                throw new Exception($"Load is not pending (current status: {load.Status})");

            var activeDrivers = await _context.Drivers
                .Where(d => d.Status == "ACTIVE")
                .ToListAsync();

            var recommendations = new List<DriverRecommendation>();

            foreach (var driver in activeDrivers)
            {
                var eligibility = await CheckDriverEligibility(driver.DriverId, loadId);

                if (eligibility.IsEligible)
                {
                    var suitability = await CalculateSuitabilityScore(driver.DriverId, loadId);
                    var overload = await CalculateOverloadScore(driver.DriverId, loadId);

                    recommendations.Add(new DriverRecommendation
                    {
                        DriverId = driver.DriverId,
                        DriverName = driver.Name,
                        Region = driver.Region,
                        VehicleType = driver.VehicleType,
                        SuitabilityScore = suitability.SuitabilityScore,
                        OverloadScore = overload.OverloadScore,
                        OverloadStatus = overload.Status,
                        FatigueScore = driver.FatigueScore,
                        RegionMatch = suitability.RegionMatch,
                        ConsecutiveDays = driver.ConsecutiveDays,
                        IsEligible = true,
                        EligibilityReason = "Eligible"
                    });
                }
                else
                {
                    recommendations.Add(new DriverRecommendation
                    {
                        DriverId = driver.DriverId,
                        DriverName = driver.Name,
                        Region = driver.Region,
                        VehicleType = driver.VehicleType,
                        SuitabilityScore = 0,
                        OverloadScore = 0,
                        OverloadStatus = "N/A",
                        FatigueScore = driver.FatigueScore,
                        RegionMatch = driver.Region.Equals(load.Region, StringComparison.OrdinalIgnoreCase),
                        ConsecutiveDays = driver.ConsecutiveDays,
                        IsEligible = false,
                        EligibilityReason = eligibility.Reason
                    });
                }
            }

            // Sort: Eligible first (by suitability desc), then ineligible
            var sortedRecommendations = recommendations
                .OrderByDescending(r => r.IsEligible)
                .ThenByDescending(r => r.SuitabilityScore)
                .ToList();

            return new AssignmentRecommendation
            {
                LoadId = loadId,
                LoadRef = load.LoadRef,
                LoadRegion = load.Region,
                LoadStops = load.Stops,
                LoadEstimatedHours = load.EstimatedHours,
                LoadPriority = load.Priority,
                EligibleDriverCount = recommendations.Count(r => r.IsEligible),
                TotalDriverCount = recommendations.Count,
                Recommendations = sortedRecommendations,
                TopRecommendation = sortedRecommendations.FirstOrDefault(r => r.IsEligible),
                GeneratedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Assigns a load to a driver (manual or auto).
        /// </summary>
        public async Task<AssignmentResult> AssignLoad(int loadId, int driverId, bool isOverride = false)
        {
            var load = await _context.Loads.FindAsync(loadId);
            var driver = await _context.Drivers.FindAsync(driverId);

            if (load == null) throw new Exception("Load not found");
            if (driver == null) throw new Exception("Driver not found");
            if (load.Status != "PENDING")
                throw new Exception($"Load is not pending (current status: {load.Status})");

            // Check eligibility unless override
            if (!isOverride)
            {
                var eligibility = await CheckDriverEligibility(driverId, loadId);
                if (!eligibility.IsEligible)
                    throw new Exception($"Driver not eligible: {eligibility.Reason}");
            }

            // Calculate scores
            var suitability = await CalculateSuitabilityScore(driverId, loadId);
            var overload = await CalculateOverloadScore(driverId, loadId);

            // Check overload unless override
            if (!isOverride && overload.Status == "UNSAFE")
                throw new Exception("Assignment would cause unsafe overload. Use override if necessary.");

            // Create shift assignment
            var assignment = new ShiftAssignment
            {
                DriverId = driverId,
                LoadId = loadId,
                LoadRef = load.LoadRef,
                AssignedDate = DateTime.UtcNow,
                Status = "ASSIGNED",
                SuitabilityScore = suitability.SuitabilityScore,
                OverloadScore = overload.OverloadScore,
                IsOverride = isOverride,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShiftAssignments.Add(assignment);

            // Update load status
            load.Status = "ASSIGNED";
            load.AssignedDriverId = driverId;
            load.AssignedAt = DateTime.UtcNow;

            // Update driver's last assignment date
            driver.LastAssignmentDate = DateTime.UtcNow;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AssignmentResult
            {
                Success = true,
                AssignmentId = assignment.AssignmentId,
                LoadId = loadId,
                LoadRef = load.LoadRef,
                DriverId = driverId,
                DriverName = driver.Name,
                SuitabilityScore = suitability.SuitabilityScore,
                OverloadScore = overload.OverloadScore,
                OverloadStatus = overload.Status,
                IsOverride = isOverride,
                Message = isOverride
                    ? "Load assigned with admin override"
                    : "Load assigned successfully"
            };
        }

        /// <summary>
        /// Auto-assigns a load to the best available driver.
        /// </summary>
        public async Task<AssignmentResult> AutoAssign(int loadId)
        {
            var recommendations = await GetRecommendations(loadId);

            if (recommendations.TopRecommendation == null)
            {
                return new AssignmentResult
                {
                    Success = false,
                    LoadId = loadId,
                    Message = "No eligible drivers available for this load"
                };
            }

            return await AssignLoad(loadId, recommendations.TopRecommendation.DriverId, isOverride: false);
        }

        #endregion
    }

    #region Result Classes

    public class OverloadResult
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public int LoadId { get; set; }
        public decimal OverloadScore { get; set; }
        public string Status { get; set; } = string.Empty; // SAFE, WARNING, UNSAFE
        public int CurrentStops { get; set; }
        public decimal CurrentHours { get; set; }
        public decimal CurrentDistance { get; set; }
        public int ProjectedStops { get; set; }
        public decimal ProjectedHours { get; set; }
        public decimal ProjectedDistance { get; set; }
        public decimal StopsNormalized { get; set; }
        public decimal HoursNormalized { get; set; }
        public decimal DistanceNormalized { get; set; }
    }

    public class SuitabilityResult
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public int LoadId { get; set; }
        public decimal SuitabilityScore { get; set; }
        public bool RegionMatch { get; set; }
        public decimal RegionScore { get; set; }
        public decimal WorkloadScore { get; set; }
        public decimal FatigueScore { get; set; }
        public decimal DistanceScore { get; set; }
        public decimal RotationPenalty { get; set; }
        public int ConsecutiveDays { get; set; }
        public decimal CurrentFatigueScore { get; set; }
    }

    public class EligibilityResult
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class DriverRecommendation
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public decimal SuitabilityScore { get; set; }
        public decimal OverloadScore { get; set; }
        public string OverloadStatus { get; set; } = string.Empty;
        public decimal FatigueScore { get; set; }
        public bool RegionMatch { get; set; }
        public int ConsecutiveDays { get; set; }
        public bool IsEligible { get; set; }
        public string EligibilityReason { get; set; } = string.Empty;
    }

    public class AssignmentRecommendation
    {
        public int LoadId { get; set; }
        public string LoadRef { get; set; } = string.Empty;
        public string LoadRegion { get; set; } = string.Empty;
        public int LoadStops { get; set; }
        public decimal LoadEstimatedHours { get; set; }
        public string LoadPriority { get; set; } = string.Empty;
        public int EligibleDriverCount { get; set; }
        public int TotalDriverCount { get; set; }
        public List<DriverRecommendation> Recommendations { get; set; } = new();
        public DriverRecommendation? TopRecommendation { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class AssignmentResult
    {
        public bool Success { get; set; }
        public int AssignmentId { get; set; }
        public int LoadId { get; set; }
        public string LoadRef { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public decimal SuitabilityScore { get; set; }
        public decimal OverloadScore { get; set; }
        public string OverloadStatus { get; set; } = string.Empty;
        public bool IsOverride { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}
