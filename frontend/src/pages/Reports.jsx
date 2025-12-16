import { useState, useEffect, useCallback } from "react";
import { FiDownloadCloud, FiRefreshCw, FiCalendar } from "react-icons/fi";
import PageHeader from "../components/PageHeader";
import ReportTabs from "../components/ReportTabs";
import CountPill from "../components/CountPill";
import * as reportService from "../services/reportService";
import { getRegions } from "../services/adminDriverService";

function Reports() {
  const [activeTab, setActiveTab] = useState("attendance");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filter states
  const [dateRange, setDateRange] = useState("7");
  const [selectedRegion, setSelectedRegion] = useState("");
  const [regions, setRegions] = useState([]);

  // Report data states
  const [attendanceData, setAttendanceData] = useState({ report: [], totalDrivers: 0 });
  const [workloadData, setWorkloadData] = useState({ report: [], totalDrivers: 0 });
  const [fatigueData, setFatigueData] = useState({ drivers: [], fatigueDistribution: {}, overtimeStats: {} });
  const [workHoursData, setWorkHoursData] = useState({ report: [], summary: {} });

  // Calculate date range
  const getDateParams = () => {
    const end = new Date();
    const start = new Date();
    start.setDate(start.getDate() - parseInt(dateRange));
    return {
      startDate: start.toISOString().split("T")[0],
      endDate: end.toISOString().split("T")[0],
      region: selectedRegion || undefined,
    };
  };

  // Fetch regions on mount
  useEffect(() => {
    const fetchRegions = async () => {
      try {
        const res = await getRegions();
        setRegions(res.data || []);
      } catch (err) {
        console.error("Failed to fetch regions:", err);
      }
    };
    fetchRegions();
  }, []);

  // Fetch data based on active tab
  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const params = getDateParams();

      switch (activeTab) {
        case "attendance": {
          const res = await reportService.getAttendanceReport(params);
          setAttendanceData(res.data);
          break;
        }
        case "load-history": {
          const res = await reportService.getWorkloadReport(params);
          setWorkloadData(res.data);
          break;
        }
        case "fatigue-trends": {
          const res = await reportService.getFatigueTrendsReport(params);
          setFatigueData(res.data);
          break;
        }
        case "work-hours": {
          const res = await reportService.getWorkHoursReport(params);
          setWorkHoursData(res.data);
          break;
        }
      }
    } catch (err) {
      console.error("Failed to fetch report data:", err);
      setError("Failed to load report data. Please try again.");
    } finally {
      setLoading(false);
    }
  }, [activeTab, dateRange, selectedRegion]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Export current report
  const handleExport = () => {
    let data = [];
    let filename = "";

    switch (activeTab) {
      case "attendance":
        data = attendanceData.report;
        filename = "attendance_report";
        break;
      case "load-history":
        data = workloadData.report;
        filename = "workload_report";
        break;
      case "fatigue-trends":
        data = fatigueData.drivers;
        filename = "fatigue_report";
        break;
      case "work-hours":
        data = workHoursData.report;
        filename = "work_hours_report";
        break;
    }

    if (data.length === 0) {
      alert("No data to export");
      return;
    }

    reportService.exportToCSV(data, filename);
  };

  const renderAttendanceReport = () => (
    <section className="card panel">
      <div className="panel-header">
        <h2 className="section-title">
          Attendance Report ({attendanceData.totalDrivers} drivers)
        </h2>
      </div>
      <div className="table-wrapper">
        <table className="table">
          <thead>
            <tr>
              <th>Driver</th>
              <th>Region</th>
              <th>Days Present</th>
              <th>Days Absent</th>
              <th>Late Check-ins</th>
              <th>Total Hours</th>
              <th>Avg Hours/Day</th>
              <th>Overtime Days</th>
            </tr>
          </thead>
          <tbody>
            {attendanceData.report.length === 0 ? (
              <tr>
                <td colSpan="8" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                  No attendance data for selected period
                </td>
              </tr>
            ) : (
              attendanceData.report.map((row) => (
                <tr key={row.driverId}>
                  <td style={{ fontWeight: 500 }}>{row.name}</td>
                  <td>{row.region}</td>
                  <td>
                    <CountPill value={row.daysPresent} variant="dark" />
                  </td>
                  <td>
                    <CountPill value={row.daysAbsent} variant={row.daysAbsent >= 2 ? "danger" : "light"} />
                  </td>
                  <td>
                    <CountPill value={row.daysLate} variant="muted" />
                  </td>
                  <td style={{ fontWeight: 500 }}>{row.totalHoursWorked}h</td>
                  <td>{row.averageHoursPerDay}h</td>
                  <td>
                    <CountPill value={row.overtimeDays} variant={row.overtimeDays > 3 ? "danger" : "light"} />
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );

  const renderWorkloadReport = () => (
    <section className="card panel">
      <div className="panel-header">
        <h2 className="section-title">
          Load History ({workloadData.totalDrivers} drivers)
        </h2>
      </div>
      <div className="table-wrapper">
        <table className="table">
          <thead>
            <tr>
              <th>Driver</th>
              <th>Region</th>
              <th>Total Loads</th>
              <th>Completed</th>
              <th>In Progress</th>
              <th>Total Stops</th>
              <th>Distance (km)</th>
              <th>Est. Hours</th>
              <th>Avg Suitability</th>
            </tr>
          </thead>
          <tbody>
            {workloadData.report.length === 0 ? (
              <tr>
                <td colSpan="9" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                  No workload data for selected period
                </td>
              </tr>
            ) : (
              workloadData.report.map((row) => (
                <tr key={row.driverId}>
                  <td style={{ fontWeight: 500 }}>{row.name}</td>
                  <td>{row.region}</td>
                  <td>
                    <CountPill value={row.totalLoadsAssigned} variant="dark" />
                  </td>
                  <td>
                    <span style={{ color: "#16a34a", fontWeight: 600 }}>{row.completedLoads}</span>
                  </td>
                  <td>
                    <span style={{ color: "#2563eb", fontWeight: 600 }}>{row.inProgressLoads}</span>
                  </td>
                  <td>{row.totalStops}</td>
                  <td>{row.totalDistance} km</td>
                  <td>{row.totalEstimatedHours}h</td>
                  <td>
                    <span style={{
                      fontWeight: 600,
                      color: row.averageSuitabilityScore >= 70 ? "#16a34a" : row.averageSuitabilityScore >= 50 ? "#ca8a04" : "#dc2626"
                    }}>
                      {row.averageSuitabilityScore}
                    </span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );

  const renderFatigueTrends = () => (
    <>
      {/* Summary Cards */}
      <section className="stats-grid" style={{ marginBottom: "24px" }}>
        <div className="card stats-card" style={{ backgroundColor: "#f0fdf4" }}>
          <div className="card-top">
            <span className="card-label">Low Fatigue</span>
          </div>
          <div className="card-value" style={{ color: "#16a34a" }}>
            {fatigueData.fatigueDistribution?.lowFatigue || 0}
          </div>
        </div>
        <div className="card stats-card" style={{ backgroundColor: "#fefce8" }}>
          <div className="card-top">
            <span className="card-label">Medium Fatigue</span>
          </div>
          <div className="card-value" style={{ color: "#ca8a04" }}>
            {fatigueData.fatigueDistribution?.mediumFatigue || 0}
          </div>
        </div>
        <div className="card stats-card" style={{ backgroundColor: "#fef2f2" }}>
          <div className="card-top">
            <span className="card-label">High Fatigue</span>
          </div>
          <div className="card-value" style={{ color: "#dc2626" }}>
            {fatigueData.fatigueDistribution?.highFatigue || 0}
          </div>
        </div>
        <div className="card stats-card" style={{ backgroundColor: "#f3f4f6" }}>
          <div className="card-top">
            <span className="card-label">Needing Rest</span>
          </div>
          <div className="card-value" style={{ color: "#374151" }}>
            {fatigueData.fatigueDistribution?.needingRest || 0}
          </div>
        </div>
      </section>

      {/* Overtime Stats */}
      <section className="card panel" style={{ marginBottom: "24px" }}>
        <div className="panel-header">
          <h2 className="section-title">Overtime Statistics</h2>
        </div>
        <div style={{ display: "flex", gap: "48px", padding: "16px 0" }}>
          <div>
            <span style={{ color: "#6b7280", fontSize: "14px" }}>Total Overtime Days</span>
            <div style={{ fontSize: "24px", fontWeight: 600 }}>{fatigueData.overtimeStats?.totalOvertimeDays || 0}</div>
          </div>
          <div>
            <span style={{ color: "#6b7280", fontSize: "14px" }}>Drivers with Overtime</span>
            <div style={{ fontSize: "24px", fontWeight: 600 }}>{fatigueData.overtimeStats?.driversWithOvertime || 0}</div>
          </div>
          <div>
            <span style={{ color: "#6b7280", fontSize: "14px" }}>Avg Overtime Hours</span>
            <div style={{ fontSize: "24px", fontWeight: 600 }}>{fatigueData.overtimeStats?.averageOvertimeHours || 0}h</div>
          </div>
          <div>
            <span style={{ color: "#6b7280", fontSize: "14px" }}>Avg Fatigue Score</span>
            <div style={{ fontSize: "24px", fontWeight: 600 }}>{fatigueData.averageFatigueScore || 0}</div>
          </div>
        </div>
      </section>

      {/* Driver Fatigue Table */}
      <section className="card panel">
        <div className="panel-header">
          <h2 className="section-title">Driver Fatigue Details</h2>
        </div>
        <div className="table-wrapper">
          <table className="table">
            <thead>
              <tr>
                <th>Driver</th>
                <th>Region</th>
                <th>Fatigue Score</th>
                <th>Level</th>
                <th>Consecutive Days</th>
                <th>Last Assignment</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {(fatigueData.drivers || []).length === 0 ? (
                <tr>
                  <td colSpan="7" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                    No fatigue data available
                  </td>
                </tr>
              ) : (
                fatigueData.drivers.map((driver) => (
                  <tr key={driver.driverId} style={{ backgroundColor: driver.needsRest ? "#fef2f2" : "transparent" }}>
                    <td style={{ fontWeight: 500 }}>{driver.name}</td>
                    <td>{driver.region}</td>
                    <td>
                      <span style={{
                        fontWeight: 600,
                        color: driver.fatigueScore > 70 ? "#dc2626" : driver.fatigueScore > 40 ? "#ca8a04" : "#16a34a"
                      }}>
                        {driver.fatigueScore}
                      </span>
                    </td>
                    <td>
                      <span style={{
                        padding: "4px 10px",
                        borderRadius: "12px",
                        fontSize: "12px",
                        fontWeight: 500,
                        backgroundColor: driver.fatigueLevel === "HIGH" ? "#fee2e2" : driver.fatigueLevel === "MEDIUM" ? "#fef3c7" : "#d1fae5",
                        color: driver.fatigueLevel === "HIGH" ? "#991b1b" : driver.fatigueLevel === "MEDIUM" ? "#92400e" : "#065f46"
                      }}>
                        {driver.fatigueLevel}
                      </span>
                    </td>
                    <td>{driver.consecutiveDays} days</td>
                    <td>{driver.lastAssignmentDate ? new Date(driver.lastAssignmentDate).toLocaleDateString() : "-"}</td>
                    <td>
                      {driver.needsRest ? (
                        <span style={{ padding: "4px 10px", borderRadius: "4px", fontSize: "12px", fontWeight: 500, backgroundColor: "#fee2e2", color: "#991b1b" }}>
                          REST REQUIRED
                        </span>
                      ) : (
                        <span style={{ padding: "4px 10px", borderRadius: "4px", fontSize: "12px", fontWeight: 500, backgroundColor: "#d1fae5", color: "#065f46" }}>
                          AVAILABLE
                        </span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </>
  );

  const renderWorkHours = () => (
    <>
      {/* Summary */}
      <section className="card panel" style={{ marginBottom: "24px" }}>
        <div className="panel-header">
          <h2 className="section-title">Work Hours Summary</h2>
        </div>
        <div style={{ display: "flex", gap: "48px", padding: "16px 0" }}>
          <div>
            <span style={{ color: "#6b7280", fontSize: "14px" }}>Total Drivers</span>
            <div style={{ fontSize: "24px", fontWeight: 600 }}>{workHoursData.summary?.totalDrivers || 0}</div>
          </div>
          <div>
            <span style={{ color: "#6b7280", fontSize: "14px" }}>Total Hours Worked</span>
            <div style={{ fontSize: "24px", fontWeight: 600 }}>{workHoursData.summary?.totalHoursWorked || 0}h</div>
          </div>
          <div>
            <span style={{ color: "#6b7280", fontSize: "14px" }}>Total Overtime</span>
            <div style={{ fontSize: "24px", fontWeight: 600, color: "#dc2626" }}>{workHoursData.summary?.totalOvertimeHours || 0}h</div>
          </div>
          <div>
            <span style={{ color: "#6b7280", fontSize: "14px" }}>Avg Hours/Driver</span>
            <div style={{ fontSize: "24px", fontWeight: 600 }}>{workHoursData.summary?.averageHoursPerDriver || 0}h</div>
          </div>
        </div>
      </section>

      {/* Detailed Table */}
      <section className="card panel">
        <div className="panel-header">
          <h2 className="section-title">Driver Work Hours</h2>
        </div>
        <div className="table-wrapper">
          <table className="table">
            <thead>
              <tr>
                <th>Driver</th>
                <th>Region</th>
                <th>Days Worked</th>
                <th>Total Hours</th>
                <th>Regular Hours</th>
                <th>Overtime Hours</th>
                <th>Avg Hours/Day</th>
                <th>Earliest In</th>
                <th>Latest Out</th>
              </tr>
            </thead>
            <tbody>
              {(workHoursData.report || []).length === 0 ? (
                <tr>
                  <td colSpan="9" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                    No work hours data for selected period
                  </td>
                </tr>
              ) : (
                workHoursData.report.map((row) => (
                  <tr key={row.driverId}>
                    <td style={{ fontWeight: 500 }}>{row.name}</td>
                    <td>{row.region}</td>
                    <td>
                      <CountPill value={row.daysWorked} variant="dark" />
                    </td>
                    <td style={{ fontWeight: 600 }}>{row.totalHours}h</td>
                    <td style={{ color: "#16a34a" }}>{row.regularHours}h</td>
                    <td style={{ color: row.overtimeHours > 0 ? "#dc2626" : "#6b7280", fontWeight: row.overtimeHours > 0 ? 600 : 400 }}>
                      {row.overtimeHours}h
                    </td>
                    <td>{row.averageHoursPerDay}h</td>
                    <td>{row.earliestCheckIn}</td>
                    <td>{row.latestCheckOut}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </>
  );

  return (
    <div className="reports-page">
      <header className="page-header" style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <div>
          <h1 className="page-title">Reports & History</h1>
          <p className="page-subtitle">Analytics and historical data</p>
        </div>
        <div style={{ display: "flex", gap: "8px" }}>
          <button
            onClick={fetchData}
            className="btn btn-secondary"
            style={{ display: "flex", alignItems: "center", gap: "6px" }}
          >
            <FiRefreshCw size={16} /> Refresh
          </button>
          <button
            onClick={handleExport}
            className="btn btn-primary"
            style={{ display: "flex", alignItems: "center", gap: "6px" }}
          >
            <FiDownloadCloud size={16} /> Export CSV
          </button>
        </div>
      </header>

      {error && (
        <div style={{ padding: "12px 16px", backgroundColor: "#fee2e2", color: "#991b1b", borderRadius: "8px", marginBottom: "16px" }}>
          {error}
        </div>
      )}

      {/* Filters */}
      <section className="card panel" style={{ marginBottom: "24px" }}>
        <div style={{ display: "flex", gap: "16px", alignItems: "center", flexWrap: "wrap" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
            <FiCalendar size={16} style={{ color: "#6b7280" }} />
            <label style={{ fontSize: "14px", color: "#374151", fontWeight: 500 }}>Date Range:</label>
            <select
              value={dateRange}
              onChange={(e) => setDateRange(e.target.value)}
              style={{
                padding: "8px 12px",
                borderRadius: "6px",
                border: "1px solid #d1d5db",
                fontSize: "14px",
                backgroundColor: "white",
                cursor: "pointer"
              }}
            >
              <option value="7">Last 7 Days</option>
              <option value="14">Last 14 Days</option>
              <option value="30">Last 30 Days</option>
              <option value="60">Last 60 Days</option>
              <option value="90">Last 90 Days</option>
            </select>
          </div>

          <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
            <label style={{ fontSize: "14px", color: "#374151", fontWeight: 500 }}>Region:</label>
            <select
              value={selectedRegion}
              onChange={(e) => setSelectedRegion(e.target.value)}
              style={{
                padding: "8px 12px",
                borderRadius: "6px",
                border: "1px solid #d1d5db",
                fontSize: "14px",
                backgroundColor: "white",
                cursor: "pointer"
              }}
            >
              <option value="">All Regions</option>
              {regions.map((region) => (
                <option key={region} value={region}>{region}</option>
              ))}
            </select>
          </div>
        </div>
      </section>

      {/* Tabs */}
      <section>
        <ReportTabs active={activeTab} onChange={setActiveTab} />
      </section>

      {/* Loading State */}
      {loading ? (
        <div style={{ padding: "40px", textAlign: "center" }}>
          <p>Loading report data...</p>
        </div>
      ) : (
        <>
          {activeTab === "attendance" && renderAttendanceReport()}
          {activeTab === "load-history" && renderWorkloadReport()}
          {activeTab === "fatigue-trends" && renderFatigueTrends()}
          {activeTab === "work-hours" && renderWorkHours()}
        </>
      )}
    </div>
  );
}

export default Reports;
