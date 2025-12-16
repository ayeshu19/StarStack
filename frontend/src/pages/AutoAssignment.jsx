import React, { useState, useEffect, useCallback } from "react";
import { FiCheckCircle, FiPackage, FiUsers, FiUserX, FiZap, FiAlertTriangle, FiRefreshCw } from "react-icons/fi";
import * as assignmentService from "../services/assignmentService";
import { getAllLoads } from "../services/loadService";

// =================== STAT CARD COMPONENT =================== //
function StatCard({ label, value, icon: Icon, variant = "default" }) {
  return (
    <div className={`card stats-card stats-card-${variant}`}>
      <div className="card-top">
        <span className="card-label">{label}</span>
        {Icon && (
          <span className={`card-icon-wrapper card-icon-${variant}`}>
            <Icon className="card-icon" />
          </span>
        )}
      </div>
      <div className="card-value">{value}</div>
    </div>
  );
}

// =================== BADGE COMPONENTS =================== //
function OverloadBadge({ status }) {
  const styles = {
    SAFE: { bg: "#d1fae5", color: "#065f46" },
    WARNING: { bg: "#fef3c7", color: "#92400e" },
    UNSAFE: { bg: "#fee2e2", color: "#991b1b" },
  };
  const style = styles[status] || styles.SAFE;
  return (
    <span style={{
      padding: "4px 10px",
      borderRadius: "12px",
      fontSize: "12px",
      fontWeight: 500,
      backgroundColor: style.bg,
      color: style.color
    }}>
      {status}
    </span>
  );
}

function FatiguePill({ score }) {
  let tone = "low";
  let label = "Low";
  if (score > 70) { tone = "high"; label = "High"; }
  else if (score > 40) { tone = "medium"; label = "Medium"; }
  return (
    <span className={`badge badge-pill badge-${tone}`}>
      {label} ({Math.round(score)})
    </span>
  );
}

function StatusBadge({ status }) {
  const styles = {
    ASSIGNED: { bg: "#fef3c7", color: "#92400e" },
    IN_PROGRESS: { bg: "#dbeafe", color: "#1e40af" },
    COMPLETED: { bg: "#d1fae5", color: "#065f46" },
  };
  const style = styles[status] || styles.ASSIGNED;
  return (
    <span style={{
      padding: "4px 10px",
      borderRadius: "4px",
      fontSize: "12px",
      fontWeight: 500,
      backgroundColor: style.bg,
      color: style.color
    }}>
      {status}
    </span>
  );
}

// =================== MAIN COMPONENT =================== //
function AutoAssignment() {
  // Use camelCase to match API response
  const [stats, setStats] = useState({
    totalAssignmentsToday: 0,
    completedToday: 0,
    inProgressToday: 0,
    assignedToday: 0,
    pendingLoads: 0,
    overrideAssignmentsToday: 0,
  });
  const [assignments, setAssignments] = useState([]);
  const [pendingLoads, setPendingLoads] = useState([]);
  const [selectedLoad, setSelectedLoad] = useState(null);
  const [recommendations, setRecommendations] = useState(null);
  const [loading, setLoading] = useState(true);
  const [assigning, setAssigning] = useState(false);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const [statsRes, assignmentsRes, loadsRes] = await Promise.all([
        assignmentService.getAssignmentStats(),
        assignmentService.getAssignments({ date: new Date().toISOString().split('T')[0] }),
        getAllLoads({ status: 'PENDING' }),
      ]);

      setStats(statsRes.data);
      setAssignments(assignmentsRes.data);
      setPendingLoads(loadsRes.data || []);
    } catch (err) {
      console.error("Failed to fetch data:", err);
      setError("Failed to load assignment data");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleGetRecommendations = async (loadId) => {
    try {
      setSelectedLoad(loadId);
      setRecommendations(null);
      const res = await assignmentService.getRecommendations(loadId);
      setRecommendations(res.data);
    } catch (err) {
      console.error("Failed to get recommendations:", err);
      alert("Failed to get recommendations: " + (err.response?.data?.error || err.message));
    }
  };

  const handleAssign = async (loadId, driverId, isOverride = false) => {
    try {
      setAssigning(true);
      await assignmentService.assignLoad(loadId, driverId, isOverride);
      alert("Load assigned successfully!");
      setSelectedLoad(null);
      setRecommendations(null);
      fetchData();
    } catch (err) {
      console.error("Failed to assign load:", err);
      alert("Failed to assign: " + (err.response?.data?.error || err.message));
    } finally {
      setAssigning(false);
    }
  };

  const handleAutoAssign = async (loadId) => {
    try {
      setAssigning(true);
      const res = await assignmentService.autoAssign(loadId);
      if (res.data.success) {
        alert(`Load auto-assigned to ${res.data.driverName}`);
        setSelectedLoad(null);
        setRecommendations(null);
        fetchData();
      } else {
        alert("Auto-assign failed: " + res.data.message);
      }
    } catch (err) {
      console.error("Auto-assign failed:", err);
      alert("Auto-assign failed: " + (err.response?.data?.error || err.message));
    } finally {
      setAssigning(false);
    }
  };

  const handleAutoAssignAll = async () => {
    if (!confirm("This will auto-assign all pending loads. Continue?")) return;
    try {
      setAssigning(true);
      const res = await assignmentService.autoAssignAll();
      alert(`Bulk assignment complete!\nSuccess: ${res.data.successCount}\nFailed: ${res.data.failedCount}`);
      fetchData();
    } catch (err) {
      console.error("Bulk auto-assign failed:", err);
      alert("Bulk auto-assign failed: " + (err.response?.data?.error || err.message));
    } finally {
      setAssigning(false);
    }
  };

  if (loading) {
    return (
      <div style={{ padding: "40px", textAlign: "center" }}>
        <p>Loading assignment data...</p>
      </div>
    );
  }

  return (
    <>
      {/* HEADER */}
      <header className="page-header" style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <div>
          <h1 className="page-title">Automatic Assignment Engine</h1>
          <p className="page-subtitle">AI-powered load assignment system</p>
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
            onClick={handleAutoAssignAll}
            className="btn btn-primary"
            disabled={assigning || pendingLoads.length === 0}
            style={{ display: "flex", alignItems: "center", gap: "6px" }}
          >
            <FiZap size={16} /> Auto-Assign All ({pendingLoads.length})
          </button>
        </div>
      </header>

      {error && (
        <div style={{ padding: "12px 16px", backgroundColor: "#fee2e2", color: "#991b1b", borderRadius: "8px", marginBottom: "16px" }}>
          {error}
        </div>
      )}

      {/* ===== STAT CARDS ===== */}
      <section className="stats-grid">
        <StatCard label="Today's Assignments" value={stats.totalAssignmentsToday} icon={FiCheckCircle} variant="green" />
        <StatCard label="Pending Loads" value={stats.pendingLoads} icon={FiPackage} variant="orange" />
        <StatCard label="In Progress" value={stats.inProgressToday} icon={FiUsers} variant="blue" />
        <StatCard label="Override Assignments" value={stats.overrideAssignmentsToday} icon={FiAlertTriangle} variant="red" />
      </section>

      {/* ===== PENDING LOADS SECTION ===== */}
      <section className="card panel" style={{ marginBottom: "24px" }}>
        <div className="panel-header">
          <h2 className="section-title">Pending Loads</h2>
          <p className="panel-subtitle">Select a load to view driver recommendations</p>
        </div>

        <div className="table-responsive">
          <table className="table">
            <thead>
              <tr>
                <th>Load Ref</th>
                <th>Region</th>
                <th>Stops</th>
                <th>Est. Hours</th>
                <th>Priority</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {pendingLoads.length === 0 ? (
                <tr>
                  <td colSpan="6" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                    No pending loads available
                  </td>
                </tr>
              ) : (
                pendingLoads.map((load) => (
                  <tr key={load.loadId} style={{ backgroundColor: selectedLoad === load.loadId ? "#f0f9ff" : "transparent" }}>
                    <td style={{ fontWeight: 500 }}>{load.loadRef}</td>
                    <td>{load.region}</td>
                    <td>{load.stops}</td>
                    <td>{load.estimatedHours}h</td>
                    <td>
                      <span style={{
                        padding: "4px 8px",
                        borderRadius: "4px",
                        fontSize: "12px",
                        fontWeight: 500,
                        backgroundColor: load.priority === "HIGH" ? "#fee2e2" : load.priority === "MEDIUM" ? "#fef3c7" : "#d1fae5",
                        color: load.priority === "HIGH" ? "#991b1b" : load.priority === "MEDIUM" ? "#92400e" : "#065f46"
                      }}>
                        {load.priority}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: "flex", gap: "8px" }}>
                        <button
                          onClick={() => handleGetRecommendations(load.loadId)}
                          className="btn btn-secondary"
                          style={{ fontSize: "12px", padding: "6px 12px" }}
                        >
                          View Recommendations
                        </button>
                        <button
                          onClick={() => handleAutoAssign(load.loadId)}
                          className="btn btn-primary"
                          disabled={assigning}
                          style={{ fontSize: "12px", padding: "6px 12px" }}
                        >
                          Auto-Assign
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      {/* ===== RECOMMENDATIONS MODAL/SECTION ===== */}
      {recommendations && (
        <section className="card panel" style={{ marginBottom: "24px", border: "2px solid #3b82f6" }}>
          <div className="panel-header" style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <div>
              <h2 className="section-title">Driver Recommendations for {recommendations.loadRef}</h2>
              <p className="panel-subtitle">
                Region: {recommendations.loadRegion} | Stops: {recommendations.loadStops} | Est. Hours: {recommendations.loadEstimatedHours}h |
                Eligible: {recommendations.eligibleDriverCount}/{recommendations.totalDriverCount} drivers
              </p>
            </div>
            <button onClick={() => { setSelectedLoad(null); setRecommendations(null); }} className="btn btn-secondary">
              Close
            </button>
          </div>

          <div className="table-responsive">
            <table className="table">
              <thead>
                <tr>
                  <th>Rank</th>
                  <th>Driver</th>
                  <th>Region</th>
                  <th>Suitability</th>
                  <th>Overload</th>
                  <th>Fatigue</th>
                  <th>Consec. Days</th>
                  <th>Status</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {recommendations.recommendations.map((driver, idx) => (
                  <tr
                    key={driver.driverId}
                    style={{
                      backgroundColor: driver.isEligible ? (idx === 0 ? "#f0fdf4" : "transparent") : "#f9fafb",
                      opacity: driver.isEligible ? 1 : 0.7
                    }}
                  >
                    <td>
                      {driver.isEligible ? (
                        <span style={{
                          display: "inline-flex",
                          alignItems: "center",
                          justifyContent: "center",
                          width: "24px",
                          height: "24px",
                          borderRadius: "50%",
                          backgroundColor: idx === 0 ? "#fbbf24" : "#e5e7eb",
                          color: idx === 0 ? "#78350f" : "#374151",
                          fontWeight: 600,
                          fontSize: "12px"
                        }}>
                          {idx + 1}
                        </span>
                      ) : "-"}
                    </td>
                    <td style={{ fontWeight: 500 }}>{driver.driverName}</td>
                    <td>
                      <span style={{ display: "flex", alignItems: "center", gap: "4px" }}>
                        {driver.region}
                        {driver.regionMatch && <span style={{ color: "#16a34a" }}>✓</span>}
                      </span>
                    </td>
                    <td>
                      {driver.isEligible ? (
                        <span style={{ fontWeight: 600, color: driver.suitabilityScore >= 70 ? "#16a34a" : driver.suitabilityScore >= 50 ? "#ca8a04" : "#dc2626" }}>
                          {driver.suitabilityScore.toFixed(1)}
                        </span>
                      ) : "-"}
                    </td>
                    <td>
                      {driver.isEligible ? <OverloadBadge status={driver.overloadStatus} /> : "-"}
                    </td>
                    <td>
                      <FatiguePill score={driver.fatigueScore} />
                    </td>
                    <td>{driver.consecutiveDays}</td>
                    <td>
                      {driver.isEligible ? (
                        <span style={{ color: "#16a34a", fontWeight: 500 }}>Eligible</span>
                      ) : (
                        <span style={{ color: "#dc2626", fontSize: "12px" }} title={driver.eligibilityReason}>
                          {driver.eligibilityReason.length > 30 ? driver.eligibilityReason.substring(0, 30) + "..." : driver.eligibilityReason}
                        </span>
                      )}
                    </td>
                    <td>
                      {driver.isEligible ? (
                        <button
                          onClick={() => handleAssign(recommendations.loadId, driver.driverId, driver.overloadStatus === "UNSAFE")}
                          className="btn btn-primary"
                          disabled={assigning}
                          style={{ fontSize: "12px", padding: "6px 12px" }}
                        >
                          {driver.overloadStatus === "UNSAFE" ? "Override & Assign" : "Assign"}
                        </button>
                      ) : (
                        <button
                          onClick={() => handleAssign(recommendations.loadId, driver.driverId, true)}
                          className="btn btn-secondary"
                          disabled={assigning}
                          style={{ fontSize: "12px", padding: "6px 12px" }}
                        >
                          Force Assign
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* ===== RECENT ASSIGNMENTS TABLE ===== */}
      <section className="card panel">
        <div className="panel-header">
          <h2 className="section-title">Today's Assignments</h2>
          <p className="panel-subtitle">All load assignments made today</p>
        </div>

        <div className="table-responsive">
          <table className="table">
            <thead>
              <tr>
                <th>Load Ref</th>
                <th>Driver</th>
                <th>Region</th>
                <th>Suitability</th>
                <th>Overload</th>
                <th>Override</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {assignments.length === 0 ? (
                <tr>
                  <td colSpan="7" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                    No assignments today
                  </td>
                </tr>
              ) : (
                assignments.map((a) => (
                  <tr key={a.assignmentId}>
                    <td style={{ fontWeight: 500 }}>{a.loadRef}</td>
                    <td>{a.driverName}</td>
                    <td>{a.loadRegion}</td>
                    <td>
                      <span style={{ fontWeight: 600, color: a.suitabilityScore >= 70 ? "#16a34a" : a.suitabilityScore >= 50 ? "#ca8a04" : "#dc2626" }}>
                        {a.suitabilityScore?.toFixed(1) || "-"}
                      </span>
                    </td>
                    <td>{a.overloadScore?.toFixed(2) || "-"}</td>
                    <td>
                      {a.isOverride ? (
                        <span style={{ color: "#dc2626", fontWeight: 500 }}>Yes</span>
                      ) : (
                        <span style={{ color: "#6b7280" }}>No</span>
                      )}
                    </td>
                    <td><StatusBadge status={a.status} /></td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </>
  );
}

export default AutoAssignment;
