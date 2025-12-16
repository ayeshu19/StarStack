import { useState, useEffect, useCallback } from "react";
import { FiUsers, FiTruck, FiAlertTriangle, FiRefreshCw } from "react-icons/fi";
import StatCard from "../components/StatCard";
import PageHeader from "../components/PageHeader";
import { StatusBadge, FatiguePill } from "../components/Badges";
import * as fatigueService from "../services/fatigueService";
import * as assignmentService from "../services/assignmentService";
import { getDriverStats } from "../services/adminDriverService";
import { getLoadStats } from "../services/loadService";

function AdminDashboard() {
  const [stats, setStats] = useState({
    activeDrivers: 0,
    pendingLoads: 0,
    assignedLoads: 0,
    highFatigueDrivers: 0,
  });
  const [assignments, setAssignments] = useState([]);
  const [alerts, setAlerts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const [driverStatsRes, loadStatsRes, assignmentsRes, alertsRes] = await Promise.all([
        getDriverStats(),
        getLoadStats(),
        assignmentService.getAssignments({ date: new Date().toISOString().split('T')[0] }),
        fatigueService.getFatigueAlerts(),
      ]);

      setStats({
        activeDrivers: driverStatsRes.data.activeDrivers || 0,
        pendingLoads: loadStatsRes.data.pendingLoads || 0,
        assignedLoads: loadStatsRes.data.assignedLoads || 0,
        highFatigueDrivers: driverStatsRes.data.highFatigueDrivers || 0,
      });

      setAssignments(assignmentsRes.data || []);
      setAlerts(alertsRes.data.alerts || []);
    } catch (err) {
      console.error("Failed to fetch dashboard data:", err);
      setError("Failed to load dashboard data. Please try again.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const getStatusTone = (status) => {
    switch (status) {
      case "COMPLETED": return "dark";
      case "IN_PROGRESS": return "blue";
      case "ASSIGNED": return "gray";
      default: return "gray";
    }
  };

  const getFatigueLabel = (score) => {
    if (score > 70) return "High";
    if (score > 40) return "Medium";
    return "Low";
  };

  if (loading) {
    return (
      <div style={{ padding: "40px", textAlign: "center" }}>
        <p>Loading dashboard...</p>
      </div>
    );
  }

  return (
    <>
      <header className="page-header" style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <div>
          <h1 className="page-title">Admin Dashboard</h1>
          <p className="page-subtitle">Monitor your logistics operations in real-time</p>
        </div>
        <button
          onClick={fetchData}
          className="btn btn-secondary"
          style={{ display: "flex", alignItems: "center", gap: "6px" }}
        >
          <FiRefreshCw size={16} /> Refresh
        </button>
      </header>

      {error && (
        <div style={{ padding: "12px 16px", backgroundColor: "#fee2e2", color: "#991b1b", borderRadius: "8px", marginBottom: "16px" }}>
          {error}
        </div>
      )}

      <section className="stats-grid">
        <StatCard
          label="Active Drivers"
          value={stats.activeDrivers}
          icon={FiUsers}
          variant="default"
        />
        <StatCard
          label="Pending Loads"
          value={stats.pendingLoads}
          icon={FiTruck}
          variant="orange"
        />
        <StatCard
          label="Assigned Loads"
          value={stats.assignedLoads}
          icon={FiTruck}
          variant="green"
        />
        <StatCard
          label="High Fatigue Drivers"
          value={stats.highFatigueDrivers}
          icon={FiAlertTriangle}
          variant="red"
        />
      </section>

      <section className="content-grid">
        <div className="card panel">
          <div className="panel-header">
            <h2 className="section-title">Today's Assignments</h2>
          </div>

          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Load Ref</th>
                  <th>Driver</th>
                  <th>Region</th>
                  <th>Status</th>
                  <th>Suitability</th>
                </tr>
              </thead>

              <tbody>
                {assignments.length === 0 ? (
                  <tr>
                    <td colSpan="5" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                      No assignments today
                    </td>
                  </tr>
                ) : (
                  assignments.slice(0, 6).map((item) => (
                    <tr key={item.assignmentId}>
                      <td style={{ fontWeight: 500 }}>{item.loadRef}</td>
                      <td>{item.driverName}</td>
                      <td>{item.loadRegion || item.driverRegion}</td>
                      <td>
                        <StatusBadge
                          label={item.status}
                          tone={getStatusTone(item.status)}
                        />
                      </td>
                      <td>
                        <span style={{
                          fontWeight: 600,
                          color: item.suitabilityScore >= 70 ? "#16a34a" : item.suitabilityScore >= 50 ? "#ca8a04" : "#dc2626"
                        }}>
                          {item.suitabilityScore?.toFixed(1) || "-"}
                        </span>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card panel alerts-panel">
          <div className="panel-header">
            <h2 className="section-title">Fatigue Alerts</h2>
          </div>

          {alerts.length === 0 ? (
            <div style={{ padding: "24px", textAlign: "center", color: "#6b7280" }}>
              No fatigue alerts
            </div>
          ) : (
            alerts.slice(0, 5).map((alert) => (
              <div
                key={alert.driverId}
                className={`alert-card alert-${alert.alertLevel === "CRITICAL" ? "high" : "medium"}`}
              >
                <div className="alert-icon">
                  <FiAlertTriangle />
                </div>

                <div className="alert-content">
                  <div className="alert-title">{alert.message}</div>
                  <div className="alert-time">
                    Consecutive days: {alert.consecutiveDays}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </section>
    </>
  );
}

export default AdminDashboard;
