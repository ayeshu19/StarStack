using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Data;
using ShiftSync.Api.Services;

namespace ShiftSync.Api.Controllers
{
    [ApiController]
    [Route("api/fatigue")]
    public class FatigueController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FatigueService _fatigueService;

        public FatigueController(AppDbContext context, FatigueService fatigueService)
        {
            _context = context;
            _fatigueService = fatigueService;
        }

        /// <summary>
        /// Get fatigue score breakdown for a specific driver.
        /// </summary>
        [HttpGet("driver/{driverId}")]
        // [Authorize(Roles = "ADMIN,DRIVER")] // Commented out for development
        public async Task<IActionResult> GetDriverFatigue(int driverId)
        {
            try
            {
                var breakdown = await _fatigueService.CalculateFatigueScore(driverId);
                return Ok(breakdown);
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Recalculate and update fatigue score for a specific driver.
        /// </summary>
        [HttpPost("calculate/{driverId}")]
        // [Authorize(Roles = "ADMIN")] // Commented out for development
        public async Task<IActionResult> CalculateDriverFatigue(int driverId)
        {
            try
            {
                var newScore = await _fatigueService.UpdateDriverFatigueScore(driverId);
                var breakdown = await _fatigueService.CalculateFatigueScore(driverId);

                return Ok(new
                {
                    message = "Fatigue score updated successfully",
                    driverId,
                    newFatigueScore = newScore,
                    breakdown
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Recalculate and update fatigue scores for all active drivers.
        /// </summary>
        [HttpPost("calculate-all")]
        // [Authorize(Roles = "ADMIN")] // Commented out for development
        public async Task<IActionResult> CalculateAllFatigue()
        {
            try
            {
                var results = await _fatigueService.UpdateAllDriverFatigueScores();

                return Ok(new
                {
                    message = "All fatigue scores updated",
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
        /// Get fatigue summary statistics for admin dashboard.
        /// </summary>
        [HttpGet("summary")]
        // [Authorize(Roles = "ADMIN")] // Commented out for development
        public async Task<IActionResult> GetFatigueSummary()
        {
            try
            {
                var summary = await _fatigueService.GetFatigueSummary();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all drivers with their fatigue details.
        /// </summary>
        [HttpGet("drivers")]
        // [Authorize(Roles = "ADMIN")] // Commented out for development
        public async Task<IActionResult> GetAllDriversFatigue()
        {
            try
            {
                var drivers = await _context.Drivers
                    .Where(d => d.Status == "ACTIVE")
                    .OrderByDescending(d => d.FatigueScore)
                    .Select(d => new
                    {
                        d.DriverId,
                        d.Name,
                        d.Region,
                        d.FatigueScore,
                        d.ConsecutiveDays,
                        d.LastAssignmentDate,
                        FatigueLevel = d.FatigueScore <= 40 ? "LOW" :
                                       d.FatigueScore <= 70 ? "MEDIUM" : "HIGH",
                        Recommendation = d.FatigueScore > 85 ? "REST REQUIRED" :
                                        d.FatigueScore > 70 ? "MONITOR CLOSELY" : "GOOD CONDITION",
                        NeedsRest = d.FatigueScore > 85
                    })
                    .ToListAsync();

                return Ok(drivers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get high fatigue alerts for admin dashboard.
        /// </summary>
        [HttpGet("alerts")]
        // [Authorize(Roles = "ADMIN")] // Commented out for development
        public async Task<IActionResult> GetFatigueAlerts()
        {
            try
            {
                var alerts = await _context.Drivers
                    .Where(d => d.Status == "ACTIVE" && d.FatigueScore > 70)
                    .OrderByDescending(d => d.FatigueScore)
                    .Select(d => new
                    {
                        d.DriverId,
                        d.Name,
                        d.Region,
                        d.FatigueScore,
                        d.ConsecutiveDays,
                        AlertLevel = d.FatigueScore > 85 ? "CRITICAL" : "WARNING",
                        Message = d.FatigueScore > 85
                            ? $"{d.Name} requires mandatory rest (Fatigue: {d.FatigueScore}%)"
                            : $"{d.Name} has elevated fatigue levels (Fatigue: {d.FatigueScore}%)"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    alertCount = alerts.Count,
                    criticalCount = alerts.Count(a => a.AlertLevel == "CRITICAL"),
                    warningCount = alerts.Count(a => a.AlertLevel == "WARNING"),
                    alerts
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
