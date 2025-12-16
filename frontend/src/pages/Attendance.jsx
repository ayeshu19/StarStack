// src/pages/Attendance.jsx
import React, { useEffect, useState } from "react";
import {
  FiCheckCircle,
  FiXCircle,
  FiClock,
  FiAlertCircle,
} from "react-icons/fi";
import StatCard from "../components/StatCard";
import PageHeader from "../components/PageHeader";
import { StatusBadge } from "../components/Badges";
import { getAttendance, getAttendanceStats } from "../services/attendanceService";

const STATUS_OPTIONS = ["All", "Present", "Absent", "Late", "Missing Checkout"];
const REGION_OPTIONS = ["All", "North", "South", "East", "West"]; // adjust to your regions

function formatTime(value) {
  if (!value) return "-";
  const d = new Date(value);
  return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

// compute worked hours from timestamps, rounded to 1 decimal
function computeHours(checkInTime, checkOutTime) {
  if (!checkInTime || !checkOutTime) return "-";
  const start = new Date(checkInTime);
  const end = new Date(checkOutTime);
  const diffMs = end.getTime() - start.getTime();
  if (isNaN(diffMs) || diffMs <= 0) return "-";
  const hours = diffMs / (1000 * 60 * 60);
  return hours.toFixed(1);
}

function Attendance() {
  const [rows, setRows] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);

  const [region, setRegion] = useState("All");
  const [statusFilter, setStatusFilter] = useState("All");

  async function loadData() {
    setLoading(true);
    try {
      const [attRes, statsRes] = await Promise.all([
        getAttendance(),
        getAttendanceStats(),
      ]);
      setRows(attRes.data);
      setStats(statsRes.data);
    } catch (err) {
      console.error("Failed to load attendance", err);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  const filteredRows = rows.filter((r) => {
    if (region !== "All" && r.region !== region) return false;
    if (statusFilter !== "All" && r.status !== statusFilter) return false;
    return true;
  });

  return (
    <>
      <PageHeader
        title="Attendance & Availability"
        subtitle="Monitor driver attendance and work hours"
        rightSlot={
          <div style={{ display: "flex", gap: "1rem", alignItems: "center" }}>
            {/* Region filter */}
            <div className="filter-dropdown">
              <span className="filter-label">Region</span>
              <select
                className="filter-control"
                value={region}
                onChange={(e) => setRegion(e.target.value)}
              >
                {REGION_OPTIONS.map((reg) => (
                  <option key={reg} value={reg}>
                    {reg}
                  </option>
                ))}
              </select>
            </div>

            {/* Status filter */}
            <div className="filter-dropdown">
              <span className="filter-label">Status</span>
              <select
                className="filter-control"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
              >
                {STATUS_OPTIONS.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
          </div>
        }
      />

      {/* Stats cards */}
      <section className="stats-grid">
        <StatCard
          label="Present Drivers"
          value={stats ? stats.presentCount : "-"}
          icon={FiCheckCircle}
          variant="green"
        />
        <StatCard
          label="Absent"
          value={stats ? stats.absentCount : "-"}
          icon={FiXCircle}
          variant="red"
        />
        <StatCard
          label="Late Check-ins"
          value={stats ? stats.lateCheckIns : "-"}
          icon={FiClock}
          variant="orange"
        />
        <StatCard
          label="Missing Check-outs"
          value={stats ? stats.missingCheckOuts : "-"}
          icon={FiAlertCircle}
          variant="orange"
        />
      </section>

      {/* Table */}
      <section className="content-full">
        <div className="card panel" style={{ width: "100%" }}>
          <div className="panel-header">
            <h2 className="section-title">Attendance</h2>
            <span className="card-label">All Dates</span>
          </div>

          <div
            className="table-wrapper"
            style={{ width: "100%", overflowX: "auto" }}
          >
            {loading ? (
              <p>Loading...</p>
            ) : filteredRows.length === 0 ? (
              <p>No attendance records found.</p>
            ) : (
              <table className="table" style={{ width: "100%" }}>
                <thead>
                  <tr>
                    <th>Driver</th>
                    <th>Region</th>
                    <th>Date</th>
                    <th>Check-In</th>
                    <th>Check-Out</th>
                    <th>Total Hours</th>
                    <th>Status</th>
                    <th>Overtime</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredRows.map((record) => (
                    <tr key={record.attendanceId}>
                      <td>{record.driverName}</td>
                      <td>{record.region}</td>
                      <td>
                        {new Date(record.date).toLocaleDateString(undefined, {
                          year: "numeric",
                          month: "short",
                          day: "numeric",
                        })}
                      </td>
                      <td
                        style={
                          record.status === "Late"
                            ? { color: "#f97316", fontWeight: 500 }
                            : {}
                        }
                      >
                        {formatTime(record.checkInTime)}
                      </td>
                      <td>{formatTime(record.checkOutTime)}</td>
                      <td>{computeHours(record.checkInTime, record.checkOutTime)}</td>
                      <td>
                        <StatusBadge
                          label={record.status}
                          tone={
                            record.status === "Present"
                              ? "dark"
                              : record.status === "Absent"
                              ? "red"
                              : record.status === "Late"
                              ? "gray"
                              : "red"
                          }
                        />
                      </td>
                      <td>
                        {record.isOvertime ? (
                          record.overtimeApproved === null ? (
                            <span
                              style={{
                                color: "#facc15",
                                fontSize: "0.875rem",
                                fontWeight: 500,
                              }}
                            >
                              Pending
                            </span>
                          ) : record.overtimeApproved ? (
                            <span
                              style={{
                                color: "#10b981",
                                fontSize: "0.875rem",
                                fontWeight: 500,
                              }}
                            >
                              Approved
                            </span>
                          ) : (
                            <span
                              style={{
                                color: "#ef4444",
                                fontSize: "0.875rem",
                                fontWeight: 500,
                              }}
                            >
                              Rejected
                            </span>
                          )
                        ) : (
                          "-"
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      </section>
    </>
  );
}

export default Attendance;