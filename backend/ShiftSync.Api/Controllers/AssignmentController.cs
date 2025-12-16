using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Data;
using ShiftSync.Api.Services;

namespace ShiftSync.Api.Controllers
{
    [ApiController]
    [Route("api/assignment")]
    // [Authorize(Roles = "ADMIN")] // Commented out for development - uncomment for production
    public class AssignmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AssignmentService _assignmentService;
        private readonly FatigueService _fatigueService;

        public AssignmentController(
            AppDbContext context,
            AssignmentService assignmentService,
            FatigueService fatigueService)
        {
            _context = context;
            _assignmentService = assignmentService;
            _fatigueService = fatigueService;
        }

        /// <summary>
        /// Get driver recommendations for a specific load.
        /// Returns all drivers ranked by suitability score.
        /// </summary>
        [HttpPost("recommend")]
        public async Task<IActionResult> GetRecommendations([FromBody] RecommendRequest request)
        {
            try
            {
                var recommendations = await _assignmentService.GetRecommendations(request.LoadId);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Manually assign a load to a specific driver.
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignLoad([FromBody] AssignRequest request)
        {
            try
            {
                var result = await _assignmentService.AssignLoad(
                    request.LoadId,
                    request.DriverId,
                    request.IsOverride);

                // Update fatigue score after assignment
                await _fatigueService.UpdateDriverFatigueScore(request.DriverId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Auto-assign a load to the best available driver.
        /// </summary>
        [HttpPost("auto-assign")]
        public async Task<IActionResult> AutoAssign([FromBody] AutoAssignRequest request)
        {
            try
            {
                var result = await _assignmentService.AutoAssign(request.LoadId);

                if (result.Success)
                {
                    // Update fatigue score after assignment
                    await _fatigueService.UpdateDriverFatigueScore(result.DriverId);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get overload prediction for a driver taking a specific load.
        /// </summary>
        [HttpGet("overload/{driverId}/{loadId}")]
        public async Task<IActionResult> GetOverloadPrediction(int driverId, int loadId)
        {
            try
            {
                var result = await _assignmentService.CalculateOverloadScore(driverId, loadId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get suitability score for a driver-load combination.
        /// </summary>
        [HttpGet("suitability/{driverId}/{loadId}")]
        public async Task<IActionResult> GetSuitabilityScore(int driverId, int loadId)
        {
            try
            {
                var result = await _assignmentService.CalculateSuitabilityScore(driverId, loadId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Check driver eligibility for a specific load.
        /// </summary>
        [HttpGet("eligibility/{driverId}/{loadId}")]
        public async Task<IActionResult> CheckEligibility(int driverId, int loadId)
        {
            try
            {
                var result = await _assignmentService.CheckDriverEligibility(driverId, loadId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all assignments with optional filters.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetAssignments(
            [FromQuery] DateTime? date,
            [FromQuery] string? status,
            [FromQuery] int? driverId)
        {
            try
            {
                var query = _context.ShiftAssignments
                    .Include(s => s.Driver)
                    .Include(s => s.Load)
                    .AsQueryable();

                if (date.HasValue)
                {
                    // Use UTC date range to avoid PostgreSQL timestamp issues
                    var dateStart = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                    var dateEnd = dateStart.AddDays(1);
                    query = query.Where(s => s.AssignedDate >= dateStart && s.AssignedDate < dateEnd);
                }

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(s => s.Status == status.ToUpper());

                if (driverId.HasValue)
                    query = query.Where(s => s.DriverId == driverId.Value);

                var assignments = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.AssignmentId,
                        s.DriverId,
                        DriverName = s.Driver != null ? s.Driver.Name : "Unknown",
                        DriverRegion = s.Driver != null ? s.Driver.Region : "",
                        s.LoadId,
                        s.LoadRef,
                        LoadRegion = s.Load != null ? s.Load.Region : "",
                        LoadStops = s.Load != null ? s.Load.Stops : 0,
                        LoadPriority = s.Load != null ? s.Load.Priority : "",
                        s.AssignedDate,
                        s.Status,
                        s.SuitabilityScore,
                        s.OverloadScore,
                        s.IsOverride,
                        s.CreatedAt
                    })
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get assignment statistics for dashboard.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetAssignmentStats()
        {
            try
            {
                // Use UTC date range to avoid PostgreSQL timestamp issues
                var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
                var todayEnd = todayStart.AddDays(1);

                var stats = new
                {
                    TotalAssignmentsToday = await _context.ShiftAssignments
                        .CountAsync(s => s.AssignedDate >= todayStart && s.AssignedDate < todayEnd),

                    CompletedToday = await _context.ShiftAssignments
                        .CountAsync(s => s.AssignedDate >= todayStart && s.AssignedDate < todayEnd && s.Status == "COMPLETED"),

                    InProgressToday = await _context.ShiftAssignments
                        .CountAsync(s => s.AssignedDate >= todayStart && s.AssignedDate < todayEnd && s.Status == "IN_PROGRESS"),

                    AssignedToday = await _context.ShiftAssignments
                        .CountAsync(s => s.AssignedDate >= todayStart && s.AssignedDate < todayEnd && s.Status == "ASSIGNED"),

                    PendingLoads = await _context.Loads
                        .CountAsync(l => l.Status == "PENDING"),

                    OverrideAssignmentsToday = await _context.ShiftAssignments
                        .CountAsync(s => s.AssignedDate >= todayStart && s.AssignedDate < todayEnd && s.IsOverride),

                    AvgSuitabilityScoreToday = await _context.ShiftAssignments
                        .Where(s => s.AssignedDate >= todayStart && s.AssignedDate < todayEnd)
                        .AverageAsync(s => (decimal?)s.SuitabilityScore) ?? 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update assignment status (ASSIGNED -> IN_PROGRESS -> COMPLETED).
        /// </summary>
        [HttpPatch("{assignmentId}/status")]
        public async Task<IActionResult> UpdateAssignmentStatus(int assignmentId, [FromBody] UpdateAssignmentStatusRequest request)
        {
            try
            {
                var assignment = await _context.ShiftAssignments
                    .Include(s => s.Load)
                    .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId);

                if (assignment == null)
                    return NotFound(new { error = "Assignment not found" });

                var validStatuses = new[] { "ASSIGNED", "IN_PROGRESS", "COMPLETED" };
                if (!validStatuses.Contains(request.Status.ToUpper()))
                    return BadRequest(new { error = "Invalid status. Use: ASSIGNED, IN_PROGRESS, or COMPLETED" });

                assignment.Status = request.Status.ToUpper();

                // Also update load status if exists
                if (assignment.Load != null)
                {
                    assignment.Load.Status = request.Status.ToUpper();
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Assignment status updated",
                    assignmentId,
                    newStatus = assignment.Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Bulk auto-assign all pending loads.
        /// </summary>
        [HttpPost("auto-assign-all")]
        public async Task<IActionResult> AutoAssignAll()
        {
            try
            {
                var pendingLoads = await _context.Loads
                    .Where(l => l.Status == "PENDING")
                    .OrderByDescending(l => l.Priority == "HIGH")
                    .ThenByDescending(l => l.Priority == "MEDIUM")
                    .ThenBy(l => l.CreatedAt)
                    .ToListAsync();

                var results = new List<AssignmentResult>();

                foreach (var load in pendingLoads)
                {
                    try
                    {
                        var result = await _assignmentService.AutoAssign(load.LoadId);
                        results.Add(result);

                        if (result.Success)
                        {
                            await _fatigueService.UpdateDriverFatigueScore(result.DriverId);
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new AssignmentResult
                        {
                            Success = false,
                            LoadId = load.LoadId,
                            LoadRef = load.LoadRef,
                            Message = ex.Message
                        });
                    }
                }

                return Ok(new
                {
                    message = "Bulk auto-assignment completed",
                    totalProcessed = results.Count,
                    successCount = results.Count(r => r.Success),
                    failedCount = results.Count(r => !r.Success),
                    results
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// [DEMO] Reset all assignments and attendance for fresh demo.
        /// </summary>
        [HttpPost("demo-reset")]
        public async Task<IActionResult> DemoReset()
        {
            try
            {
                // 1. Delete all shift assignments
                var assignments = await _context.ShiftAssignments.ToListAsync();
                _context.ShiftAssignments.RemoveRange(assignments);

                // 2. Reset all loads to PENDING status
                var loads = await _context.Loads.ToListAsync();
                foreach (var load in loads)
                {
                    load.Status = "PENDING";
                    load.AssignedDriverId = null;
                    load.AssignedAt = null;
                }

                // 3. Delete all attendance records
                var attendances = await _context.Attendances.ToListAsync();
                _context.Attendances.RemoveRange(attendances);

                // 4. Reset all driver fatigue scores and consecutive days
                var drivers = await _context.Drivers.ToListAsync();
                foreach (var driver in drivers)
                {
                    driver.FatigueScore = 0;
                    driver.ConsecutiveDays = 0;
                    driver.LastAssignmentDate = null;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Demo reset completed successfully!",
                    assignmentsDeleted = assignments.Count,
                    loadsReset = loads.Count,
                    attendancesDeleted = attendances.Count,
                    driversReset = drivers.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// [DEBUG] Check eligibility of all drivers for a specific load.
        /// </summary>
        [HttpGet("debug-eligibility/{loadId}")]
        public async Task<IActionResult> DebugEligibility(int loadId)
        {
            try
            {
                var load = await _context.Loads.FindAsync(loadId);
                if (load == null)
                    return NotFound(new { error = "Load not found" });

                var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
                var todayDayOfWeek = DateTime.UtcNow.DayOfWeek.ToString();

                var drivers = await _context.Drivers
                    .Where(d => d.Status == "ACTIVE")
                    .ToListAsync();

                // Get all attendances for today (in-memory filter)
                var allAttendances = await _context.Attendances.ToListAsync();
                var todayAttendances = allAttendances
                    .Where(a => a.Date.Date == todayStart.Date)
                    .ToList();

                var debugInfo = new List<object>();

                foreach (var driver in drivers)
                {
                    var attendance = todayAttendances.FirstOrDefault(a => a.DriverId == driver.DriverId);
                    var eligibility = await _assignmentService.CheckDriverEligibility(driver.DriverId, loadId);

                    debugInfo.Add(new
                    {
                        driver.DriverId,
                        driver.Name,
                        driver.Region,
                        driver.WeeklyOff,
                        driver.FatigueScore,
                        driver.Status,
                        TodayIs = todayDayOfWeek,
                        IsWeeklyOff = driver.WeeklyOff.Equals(todayDayOfWeek, StringComparison.OrdinalIgnoreCase),
                        HasAttendanceRecord = attendance != null,
                        CheckedIn = attendance?.CheckInTime != null,
                        AttendanceDate = attendance?.Date.ToString("yyyy-MM-dd"),
                        eligibility.IsEligible,
                        eligibility.Reason
                    });
                }

                return Ok(new
                {
                    loadId,
                    loadRegion = load.Region,
                    todayDate = todayStart.ToString("yyyy-MM-dd"),
                    todayDayOfWeek,
                    totalDrivers = drivers.Count,
                    eligibleCount = debugInfo.Count(d => ((dynamic)d).IsEligible),
                    drivers = debugInfo
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, details = ex.ToString() });
            }
        }
    }

    // Request DTOs
    public class RecommendRequest
    {
        public int LoadId { get; set; }
    }

    public class AssignRequest
    {
        public int LoadId { get; set; }
        public int DriverId { get; set; }
        public bool IsOverride { get; set; } = false;
    }

    public class AutoAssignRequest
    {
        public int LoadId { get; set; }
    }

    public class UpdateAssignmentStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
