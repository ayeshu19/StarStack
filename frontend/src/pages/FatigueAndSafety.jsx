import { useState, useEffect, useCallback } from "react";
import { FiCheckCircle, FiAlertTriangle, FiAlertCircle, FiUsers, FiRefreshCw, FiActivity } from "react-icons/fi";
import * as fatigueService from "../services/fatigueService";

// =================== BADGE COMPONENTS =================== //
function FatiguePill({ level }) {
  const styles = {
    LOW: { bg: "#d1fae5", color: "#065f46" },
    MEDIUM: { bg: "#fef3c7", color: "#92400e" },
    HIGH: { bg: "#fee2e2", color: "#991b1b" },
  };
  const style = styles[level] || styles.LOW;
  return (
    <span style={{
      padding: "4px 10px",
      borderRadius: "12px",
      fontSize: "12px",
      fontWeight: 500,
      backgroundColor: style.bg,
      color: style.color
    }}>
      {level}
    </span>
  );
}

function RecommendationBadge({ recommendation }) {
  let bg = "#d1fae5";
  let color = "#065f46";
  if (recommendation === "REST REQUIRED") {
    bg = "#fee2e2";
    color = "#991b1b";
  } else if (recommendation === "MONITOR CLOSELY") {
    bg = "#fef3c7";
    color = "#92400e";
  }
  return (
    <span style={{
      padding: "4px 10px",
      borderRadius: "4px",
      fontSize: "12px",
      fontWeight: 500,
      backgroundColor: bg,
      color: color
    }}>
      {recommendation}
    </span>
  );
}

// =================== STAT CARD COMPONENT =================== //
function StatCard({ label, value, icon: Icon, color, bgColor }) {
  return (
    <div className="card stats-card" style={{ backgroundColor: bgColor || "#f9fafb" }}>
      <div className="card-top">
        <span className="card-label" style={{ color: "#374151" }}>{label}</span>
        {Icon && (
          <span style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            width: "36px",
            height: "36px",
            borderRadius: "8px",
            backgroundColor: color,
            color: "white"
          }}>
            <Icon size={18} />
          </span>
        )}
      </div>
      <div className="card-value" style={{ color: "#111827", fontSize: "28px", fontWeight: 700 }}>{value}</div>
    </div>
  );
}

// =================== BAR CHART COMPONENT =================== //
function FatigueBarChart({ drivers }) {
  if (!drivers || drivers.length === 0) {
    return (
      <div style={{ padding: "60px", textAlign: "center", color: "#6b7280" }}>
        <FiActivity size={48} style={{ marginBottom: "16px", opacity: 0.5 }} />
        <p>No driver data available for visualization</p>
        <p style={{ fontSize: "14px" }}>Upload drivers and check them in to see fatigue data</p>
      </div>
    );
  }

  const maxScore = 100;
  const chartHeight = 320;

  return (
    <div style={{ padding: "24px" }}>
      {/* Legend */}
      <div style={{ display: "flex", justifyContent: "center", gap: "24px", marginBottom: "24px" }}>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <div style={{ width: "16px", height: "16px", borderRadius: "4px", backgroundColor: "#22c55e" }}></div>
          <span style={{ fontSize: "13px", color: "#374151" }}>Low (0-40)</span>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <div style={{ width: "16px", height: "16px", borderRadius: "4px", backgroundColor: "#eab308" }}></div>
          <span style={{ fontSize: "13px", color: "#374151" }}>Medium (41-70)</span>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <div style={{ width: "16px", height: "16px", borderRadius: "4px", backgroundColor: "#ef4444" }}></div>
          <span style={{ fontSize: "13px", color: "#374151" }}>High (71+)</span>
        </div>
      </div>

      {/* Chart Container */}
      <div style={{ display: "flex", alignItems: "flex-end", position: "relative" }}>
        {/* Y-Axis Labels */}
        <div style={{
          display: "flex",
          flexDirection: "column",
          justifyContent: "space-between",
          height: `${chartHeight}px`,
          paddingRight: "12px",
          borderRight: "1px solid #e5e7eb"
        }}>
          <span style={{ fontSize: "11px", color: "#6b7280" }}>100</span>
          <span style={{ fontSize: "11px", color: "#6b7280" }}>75</span>
          <span style={{ fontSize: "11px", color: "#6b7280" }}>50</span>
          <span style={{ fontSize: "11px", color: "#6b7280" }}>25</span>
          <span style={{ fontSize: "11px", color: "#6b7280" }}>0</span>
        </div>

        {/* Bars Container */}
        <div style={{
          flex: 1,
          display: "flex",
          justifyContent: "space-around",
          alignItems: "flex-end",
          height: `${chartHeight}px`,
          paddingLeft: "16px",
          position: "relative"
        }}>
          {/* Grid Lines */}
          <div style={{ position: "absolute", top: 0, left: "16px", right: 0, height: "100%", pointerEvents: "none" }}>
            {[0, 25, 50, 75, 100].map((val) => (
              <div
                key={val}
                style={{
                  position: "absolute",
                  bottom: `${(val / maxScore) * 100}%`,
                  left: 0,
                  right: 0,
                  height: "1px",
                  backgroundColor: "#f3f4f6"
                }}
              />
            ))}
            {/* Danger Zone Line at 85 */}
            <div
              style={{
                position: "absolute",
                bottom: `${(85 / maxScore) * 100}%`,
                left: 0,
                right: 0,
                height: "2px",
                backgroundColor: "#fca5a5",
                borderStyle: "dashed"
              }}
            />
          </div>

          {/* Bars */}
          {drivers.slice(0, 15).map((driver, index) => {
            let barColor = "#22c55e";
            if (driver.fatigueScore > 70) barColor = "#ef4444";
            else if (driver.fatigueScore > 40) barColor = "#eab308";

            const barHeight = (driver.fatigueScore / maxScore) * chartHeight;

            return (
              <div
                key={driver.driverId}
                style={{
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "center",
                  position: "relative",
                  zIndex: 1
                }}
              >
                {/* Score Label */}
                <span style={{
                  fontSize: "11px",
                  fontWeight: 600,
                  color: "#374151",
                  marginBottom: "4px"
                }}>
                  {Math.round(driver.fatigueScore)}
                </span>

                {/* Bar */}
                <div
                  style={{
                    width: "40px",
                    height: `${Math.max(barHeight, 8)}px`,
                    backgroundColor: barColor,
                    borderRadius: "4px 4px 0 0",
                    transition: "height 0.5s ease, background-color 0.3s ease",
                    cursor: "pointer",
                    position: "relative"
                  }}
                  title={`${driver.name}: ${driver.fatigueScore}% fatigue\n${driver.consecutiveDays} consecutive days\n${driver.recommendation}`}
                >
                  {driver.needsRest && (
                    <div style={{
                      position: "absolute",
                      top: "-8px",
                      left: "50%",
                      transform: "translateX(-50%)",
                      color: "#dc2626",
                      fontSize: "14px"
                    }}>
                      !
                    </div>
                  )}
                </div>

                {/* Driver Name */}
                <span style={{
                  fontSize: "10px",
                  color: "#6b7280",
                  marginTop: "8px",
                  textAlign: "center",
                  maxWidth: "50px",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap"
                }}>
                  {driver.name.split(' ')[0]}
                </span>
              </div>
            );
          })}
        </div>
      </div>

      {/* X-Axis Label */}
      <div style={{ textAlign: "center", marginTop: "16px", color: "#6b7280", fontSize: "12px" }}>
        Drivers (showing top {Math.min(drivers.length, 15)} by fatigue score)
      </div>
    </div>
  );
}

// =================== PIE/DONUT CHART COMPONENT =================== //
function FatigueDistributionChart({ low, medium, high }) {
  const total = low + medium + high;
  if (total === 0) return null;

  const lowPct = (low / total) * 100;
  const mediumPct = (medium / total) * 100;
  const highPct = (high / total) * 100;

  return (
    <div style={{ display: "flex", alignItems: "center", gap: "24px", padding: "16px" }}>
      {/* Simple Stacked Bar */}
      <div style={{ flex: 1 }}>
        <div style={{
          display: "flex",
          height: "24px",
          borderRadius: "12px",
          overflow: "hidden",
          backgroundColor: "#f3f4f6"
        }}>
          {lowPct > 0 && (
            <div style={{ width: `${lowPct}%`, backgroundColor: "#22c55e", transition: "width 0.5s ease" }} />
          )}
          {mediumPct > 0 && (
            <div style={{ width: `${mediumPct}%`, backgroundColor: "#eab308", transition: "width 0.5s ease" }} />
          )}
          {highPct > 0 && (
            <div style={{ width: `${highPct}%`, backgroundColor: "#ef4444", transition: "width 0.5s ease" }} />
          )}
        </div>
        <div style={{ display: "flex", justifyContent: "space-between", marginTop: "8px", fontSize: "12px", color: "#6b7280" }}>
          <span>Low: {low} ({lowPct.toFixed(0)}%)</span>
          <span>Medium: {medium} ({mediumPct.toFixed(0)}%)</span>
          <span>High: {high} ({highPct.toFixed(0)}%)</span>
        </div>
      </div>
    </div>
  );
}

// =================== MAIN COMPONENT =================== //
function FatigueSafety() {
  const [viewMode, setViewMode] = useState("visual");
  const [summary, setSummary] = useState({
    totalActiveDrivers: 0,
    lowFatigueCount: 0,
    mediumFatigueCount: 0,
    highFatigueCount: 0,
    driversNeedingRest: 0,
    averageFatigueScore: 0,
  });
  const [drivers, setDrivers] = useState([]);
  const [alerts, setAlerts] = useState({ alertCount: 0, alerts: [] });
  const [loading, setLoading] = useState(true);
  const [calculating, setCalculating] = useState(false);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const [summaryRes, driversRes, alertsRes] = await Promise.all([
        fatigueService.getFatigueSummary(),
        fatigueService.getAllDriversFatigue(),
        fatigueService.getFatigueAlerts(),
      ]);

      setSummary(summaryRes.data);
      setDrivers(driversRes.data || []);
      setAlerts(alertsRes.data);
    } catch (err) {
      console.error("Failed to fetch fatigue data:", err);
      setError("Failed to load fatigue data. Make sure the backend is running.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleRecalculateAll = async () => {
    if (!confirm("This will recalculate fatigue scores for all drivers. Continue?")) return;
    try {
      setCalculating(true);
      const res = await fatigueService.calculateAllFatigue();
      alert(`Recalculation complete!\nSuccess: ${res.data.successCount}\nFailed: ${res.data.failedCount}`);
      fetchData();
    } catch (err) {
      console.error("Failed to recalculate:", err);
      alert("Recalculation failed: " + (err.response?.data?.error || err.message));
    } finally {
      setCalculating(false);
    }
  };

  const handleRecalculateDriver = async (driverId) => {
    try {
      setCalculating(true);
      await fatigueService.calculateDriverFatigue(driverId);
      fetchData();
    } catch (err) {
      console.error("Failed to recalculate:", err);
      alert("Recalculation failed: " + (err.response?.data?.error || err.message));
    } finally {
      setCalculating(false);
    }
  };

  if (loading) {
    return (
      <div style={{ padding: "60px", textAlign: "center" }}>
        <FiActivity size={48} style={{ marginBottom: "16px", color: "#6b7280", animation: "pulse 2s infinite" }} />
        <p style={{ color: "#6b7280" }}>Loading fatigue data...</p>
      </div>
    );
  }

  return (
    <>
      {/* HEADER */}
      <header className="page-header" style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <div>
          <h1 className="page-title">Fatigue & Safety Monitor</h1>
          <p className="page-subtitle">Track driver wellness and prevent overwork incidents</p>
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
            onClick={handleRecalculateAll}
            className="btn btn-primary"
            disabled={calculating}
            style={{ display: "flex", alignItems: "center", gap: "6px" }}
          >
            {calculating ? "Calculating..." : "Recalculate All Scores"}
          </button>
        </div>
      </header>

      {/* ERROR MESSAGE */}
      {error && (
        <div style={{ padding: "12px 16px", backgroundColor: "#fee2e2", color: "#991b1b", borderRadius: "8px", marginBottom: "16px" }}>
          {error}
        </div>
      )}

      {/* ALERTS SECTION */}
      {alerts.alertCount > 0 && (
        <section className="card panel" style={{ marginBottom: "24px", border: "2px solid #ef4444" }}>
          <div className="panel-header" style={{ borderBottom: "1px solid #fecaca", paddingBottom: "12px" }}>
            <h2 className="section-title" style={{ color: "#dc2626", display: "flex", alignItems: "center", gap: "8px" }}>
              <FiAlertCircle /> Fatigue Alerts ({alerts.alertCount})
            </h2>
            <p style={{ fontSize: "13px", color: "#991b1b", marginTop: "4px" }}>
              {alerts.criticalCount || 0} critical, {alerts.warningCount || 0} warnings - Immediate attention required
            </p>
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: "8px", marginTop: "12px" }}>
            {alerts.alerts.map((alert) => (
              <div
                key={alert.driverId}
                style={{
                  padding: "12px 16px",
                  backgroundColor: alert.alertLevel === "CRITICAL" ? "#fee2e2" : "#fef3c7",
                  borderRadius: "8px",
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  borderLeft: `4px solid ${alert.alertLevel === "CRITICAL" ? "#dc2626" : "#d97706"}`
                }}
              >
                <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
                  <FiAlertTriangle style={{ color: alert.alertLevel === "CRITICAL" ? "#dc2626" : "#d97706" }} />
                  <div>
                    <span style={{ fontWeight: 600, color: alert.alertLevel === "CRITICAL" ? "#991b1b" : "#92400e" }}>
                      {alert.alertLevel}:
                    </span>{" "}
                    <span style={{ color: "#374151" }}>{alert.message}</span>
                  </div>
                </div>
                <span style={{
                  padding: "4px 12px",
                  borderRadius: "16px",
                  fontSize: "12px",
                  fontWeight: 600,
                  backgroundColor: alert.alertLevel === "CRITICAL" ? "#dc2626" : "#d97706",
                  color: "white"
                }}>
                  {alert.fatigueScore}%
                </span>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* STAT CARDS */}
      <section className="stats-grid">
        <StatCard
          label="Low Fatigue (0-40)"
          value={summary.lowFatigueCount}
          icon={FiCheckCircle}
          color="#22c55e"
          bgColor="#f0fdf4"
        />
        <StatCard
          label="Medium Fatigue (41-70)"
          value={summary.mediumFatigueCount}
          icon={FiAlertTriangle}
          color="#eab308"
          bgColor="#fefce8"
        />
        <StatCard
          label="High Fatigue (71+)"
          value={summary.highFatigueCount}
          icon={FiAlertCircle}
          color="#ef4444"
          bgColor="#fef2f2"
        />
        <StatCard
          label="Need Rest (85+)"
          value={summary.driversNeedingRest}
          icon={FiUsers}
          color="#6b7280"
          bgColor="#f3f4f6"
        />
      </section>

      {/* DISTRIBUTION BAR */}
      <section className="card panel" style={{ marginBottom: "24px" }}>
        <div className="panel-header">
          <h2 className="section-title">Fatigue Distribution</h2>
        </div>
        <FatigueDistributionChart
          low={summary.lowFatigueCount}
          medium={summary.mediumFatigueCount}
          high={summary.highFatigueCount}
        />
        <div style={{ padding: "0 16px 16px", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <span style={{ fontSize: "14px", color: "#6b7280" }}>
            Total Active Drivers: <strong style={{ color: "#111827" }}>{summary.totalActiveDrivers}</strong>
          </span>
          <span style={{ fontSize: "14px", color: "#6b7280" }}>
            Average Fatigue Score: <strong style={{ color: summary.averageFatigueScore > 70 ? "#dc2626" : summary.averageFatigueScore > 40 ? "#d97706" : "#16a34a" }}>
              {summary.averageFatigueScore}%
            </strong>
          </span>
        </div>
      </section>

      {/* MAIN DATA SECTION */}
      <section className="card panel">
        <div className="panel-header" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
          <h2 className="section-title">Driver Fatigue Details</h2>
          <div style={{ display: "flex", gap: "16px", alignItems: "center" }}>
            <label style={{ display: "flex", alignItems: "center", gap: "6px", cursor: "pointer" }}>
              <input
                type="radio"
                name="viewMode"
                checked={viewMode === "visual"}
                onChange={() => setViewMode("visual")}
                style={{ cursor: "pointer" }}
              />
              <span style={{ fontSize: "14px", fontWeight: viewMode === "visual" ? "600" : "400", color: "#374151" }}>
                Chart View
              </span>
            </label>
            <label style={{ display: "flex", alignItems: "center", gap: "6px", cursor: "pointer" }}>
              <input
                type="radio"
                name="viewMode"
                checked={viewMode === "table"}
                onChange={() => setViewMode("table")}
                style={{ cursor: "pointer" }}
              />
              <span style={{ fontSize: "14px", fontWeight: viewMode === "table" ? "600" : "400", color: "#374151" }}>
                Table View
              </span>
            </label>
          </div>
        </div>

        {viewMode === "visual" ? (
          <FatigueBarChart drivers={drivers} />
        ) : (
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
                  <th>Recommendation</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {drivers.length === 0 ? (
                  <tr>
                    <td colSpan="8" style={{ textAlign: "center", padding: "48px", color: "#6b7280" }}>
                      <FiUsers size={32} style={{ marginBottom: "12px", opacity: 0.5 }} />
                      <p>No driver data available</p>
                      <p style={{ fontSize: "13px" }}>Upload drivers via CSV to get started</p>
                    </td>
                  </tr>
                ) : (
                  drivers.map((driver) => (
                    <tr key={driver.driverId} style={{ backgroundColor: driver.needsRest ? "#fef2f2" : "transparent" }}>
                      <td style={{ fontWeight: 500 }}>
                        {driver.name}
                        {driver.needsRest && (
                          <span style={{ marginLeft: "8px", color: "#dc2626", fontSize: "12px" }}>REST</span>
                        )}
                      </td>
                      <td>{driver.region}</td>
                      <td>
                        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                          <div style={{
                            width: "60px",
                            height: "8px",
                            backgroundColor: "#e5e7eb",
                            borderRadius: "4px",
                            overflow: "hidden"
                          }}>
                            <div style={{
                              width: `${driver.fatigueScore}%`,
                              height: "100%",
                              backgroundColor: driver.fatigueScore > 70 ? "#ef4444" : driver.fatigueScore > 40 ? "#eab308" : "#22c55e",
                              transition: "width 0.3s ease"
                            }} />
                          </div>
                          <span style={{
                            fontWeight: 600,
                            color: driver.fatigueScore > 70 ? "#dc2626" : driver.fatigueScore > 40 ? "#ca8a04" : "#16a34a"
                          }}>
                            {driver.fatigueScore}%
                          </span>
                        </div>
                      </td>
                      <td><FatiguePill level={driver.fatigueLevel} /></td>
                      <td>
                        <span style={{
                          fontWeight: driver.consecutiveDays >= 6 ? 600 : 400,
                          color: driver.consecutiveDays >= 6 ? "#dc2626" : "#374151"
                        }}>
                          {driver.consecutiveDays} days
                        </span>
                      </td>
                      <td style={{ color: "#6b7280" }}>
                        {driver.lastAssignmentDate
                          ? new Date(driver.lastAssignmentDate).toLocaleDateString()
                          : "-"}
                      </td>
                      <td><RecommendationBadge recommendation={driver.recommendation} /></td>
                      <td>
                        <button
                          onClick={() => handleRecalculateDriver(driver.driverId)}
                          className="btn btn-secondary"
                          disabled={calculating}
                          style={{ fontSize: "12px", padding: "6px 10px" }}
                        >
                          Recalculate
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}

export default FatigueSafety;
