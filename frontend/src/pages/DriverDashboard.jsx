import React, { useState, useEffect, useCallback } from "react";
import { FiClock, FiPackage, FiActivity, FiMapPin, FiLogOut, FiTruck, FiKey, FiRefreshCw, FiAlertCircle } from "react-icons/fi";
import { useNavigate } from "react-router-dom";
import StatCard from "../components/StatCard";
import { getDriverDashboard, checkInDriver, checkOutDriver } from "../services/driverService";

function DriverDashboard() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [dashboardData, setDashboardData] = useState(null);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");

  const handleLogout = useCallback(() => {
    localStorage.removeItem("token");
    localStorage.removeItem("userRole");
    localStorage.removeItem("userId");
    localStorage.removeItem("driverId");
    localStorage.removeItem("driverData");
    navigate("/");
  }, [navigate]);

  const fetchDashboardData = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const data = await getDriverDashboard();
      setDashboardData(data);
    } catch (err) {
      console.error("Dashboard error:", err);
      if (err.response?.status === 401) {
        handleLogout();
      } else {
        setError(err.response?.data?.message || "Failed to load dashboard. Please try again.");
      }
    } finally {
      setLoading(false);
    }
  }, [handleLogout]);

  useEffect(() => {
    const userRole = localStorage.getItem("userRole");
    const token = localStorage.getItem("token");

    if (userRole !== "DRIVER" || !token) {
      navigate("/");
      return;
    }

    fetchDashboardData();
  }, [navigate, fetchDashboardData]);

  const handleCheckIn = async () => {
    try {
      setActionLoading(true);
      setActionError("");
      await checkInDriver();
      await fetchDashboardData();
    } catch (err) {
      console.error("Check-in error:", err);
      setActionError(err.response?.data?.message || "Check-in failed. Please try again.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleCheckOut = async () => {
    try {
      setActionLoading(true);
      setActionError("");
      await checkOutDriver();
      await fetchDashboardData();
    } catch (err) {
      console.error("Check-out error:", err);
      setActionError(err.response?.data?.message || "Check-out failed. Please try again.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleResetPassword = () => {
    navigate("/driver/reset-password");
  };

  // Determine check-in state from attendance data
  const isCheckedIn = dashboardData?.todayAttendance?.checkInTime && !dashboardData?.todayAttendance?.checkOutTime;
  const hasCheckedOut = dashboardData?.todayAttendance?.checkInTime && dashboardData?.todayAttendance?.checkOutTime;

  // Calculate hours worked
  const calculateHoursWorked = () => {
    const attendance = dashboardData?.todayAttendance;
    if (!attendance?.checkInTime) return "0h 0m";

    const checkIn = new Date(attendance.checkInTime);
    const checkOut = attendance.checkOutTime ? new Date(attendance.checkOutTime) : new Date();
    const diffMs = checkOut - checkIn;
    const hours = Math.floor(diffMs / (1000 * 60 * 60));
    const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
    return `${hours}h ${minutes}m`;
  };

  const formatTime = (dateString) => {
    if (!dateString) return "N/A";
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  };

  // Get status color for assignments
  const getStatusStyle = (status) => {
    const upperStatus = status?.toUpperCase() || "";
    if (upperStatus === "COMPLETED" || upperStatus === "COMPLETE") {
      return { background: '#d1fae5', color: '#065f46' };
    } else if (upperStatus === "IN_PROGRESS" || upperStatus === "IN PROGRESS" || upperStatus === "INPROGRESS") {
      return { background: '#dbeafe', color: '#1e40af' };
    } else {
      return { background: '#fef3c7', color: '#92400e' }; // Pending/Assigned/Default
    }
  };

  // Loading state
  if (loading) {
    return (
      <div style={{
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
        gap: '16px',
        backgroundColor: '#f9fafb'
      }}>
        <div style={{
          width: '48px',
          height: '48px',
          border: '4px solid #e5e7eb',
          borderTopColor: '#3b82f6',
          borderRadius: '50%',
          animation: 'spin 1s linear infinite'
        }} />
        <p style={{ fontSize: '16px', color: '#6b7280' }}>Loading your dashboard...</p>
        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      </div>
    );
  }

  // Error state with retry
  if (error) {
    return (
      <div style={{
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
        gap: '20px',
        padding: '24px',
        backgroundColor: '#f9fafb'
      }}>
        <div style={{
          width: '64px',
          height: '64px',
          background: '#fee2e2',
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: '#dc2626',
          fontSize: '28px'
        }}>
          <FiAlertCircle />
        </div>
        <h2 style={{ fontSize: '20px', fontWeight: '600', color: '#111827', margin: 0 }}>
          Something went wrong
        </h2>
        <p style={{ color: '#6b7280', fontSize: '14px', textAlign: 'center', maxWidth: '400px', margin: 0 }}>
          {error}
        </p>
        <button
          onClick={fetchDashboardData}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            padding: '12px 24px',
            background: '#111827',
            color: '#fff',
            border: 'none',
            borderRadius: '8px',
            fontSize: '14px',
            fontWeight: '500',
            cursor: 'pointer'
          }}
        >
          <FiRefreshCw />
          Try Again
        </button>
        <button
          onClick={handleLogout}
          style={{
            padding: '8px 16px',
            background: 'none',
            color: '#6b7280',
            border: 'none',
            fontSize: '14px',
            cursor: 'pointer',
            textDecoration: 'underline'
          }}
        >
          Back to Login
        </button>
      </div>
    );
  }

  const driverName = dashboardData?.name || "Driver";
  const fatigueScore = dashboardData?.fatigueScore || 0;
  const vehicleType = dashboardData?.vehicleType || "N/A";
  const weeklyOff = dashboardData?.weeklyOff || "N/A";
  const region = dashboardData?.region || "N/A";
  const attendance = dashboardData?.todayAttendance;
  const assignments = dashboardData?.assignments || [];

  return (
    <div style={{ padding: '16px', maxWidth: '1400px', margin: '0 auto', backgroundColor: '#f9fafb', minHeight: '100vh' }}>
      {/* Top Navigation Bar */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '12px 0',
        marginBottom: '20px',
        borderBottom: '2px solid #e5e7eb',
        flexWrap: 'wrap',
        gap: '12px'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <div style={{
            width: '36px',
            height: '36px',
            borderRadius: '8px',
            background: '#111827',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#fff',
            fontSize: '18px'
          }}>
            <FiTruck />
          </div>
          <span style={{ fontSize: '18px', fontWeight: '600', color: '#111827' }}>ShiftSync</span>
        </div>
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
          <button
            onClick={handleResetPassword}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 12px',
              background: 'none',
              border: '1px solid #e5e7eb',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '13px',
              color: '#374151',
              transition: 'all 0.2s'
            }}
            onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#f3f4f6'}
            onMouseOut={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
          >
            <FiKey size={14} />
            <span>Reset Password</span>
          </button>
          <button
            onClick={handleLogout}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 12px',
              background: 'none',
              border: '1px solid #fecaca',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '13px',
              color: '#dc2626',
              transition: 'all 0.2s'
            }}
            onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#fee2e2'}
            onMouseOut={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
          >
            <FiLogOut size={14} />
            <span>Logout</span>
          </button>
        </div>
      </div>

      {/* Welcome Header */}
      <div style={{ marginBottom: '24px' }}>
        <h1 style={{ fontSize: '24px', fontWeight: '600', color: '#111827', margin: '0 0 6px 0' }}>
          Welcome back, {driverName}!
        </h1>
        <p style={{ fontSize: '13px', color: '#6b7280', margin: 0 }}>
          {new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
        </p>
      </div>

      {/* Action Error Alert */}
      {actionError && (
        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
          padding: '12px 16px',
          background: '#fee2e2',
          borderRadius: '8px',
          marginBottom: '20px',
          border: '1px solid #fecaca'
        }}>
          <FiAlertCircle style={{ color: '#dc2626', flexShrink: 0 }} />
          <p style={{ color: '#991b1b', fontSize: '14px', margin: 0, flex: 1 }}>{actionError}</p>
          <button
            onClick={() => setActionError("")}
            style={{ background: 'none', border: 'none', color: '#991b1b', cursor: 'pointer', fontSize: '18px' }}
          >
            &times;
          </button>
        </div>
      )}

      {/* Stats Cards */}
      <section style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
        gap: '12px',
        marginBottom: '24px'
      }}>
        <StatCard label="Vehicle Type" value={vehicleType} icon={FiTruck} variant="default" />
        <StatCard label="Region" value={region} icon={FiMapPin} variant="blue" />
        <StatCard label="Weekly Off" value={weeklyOff} icon={FiClock} variant="default" />
        <StatCard
          label="Fatigue Score"
          value={`${parseFloat(fatigueScore).toFixed(1)}/10`}
          icon={FiActivity}
          variant={fatigueScore > 7 ? "red" : fatigueScore > 5 ? "yellow" : "green"}
        />
      </section>

      {/* Main Content based on check-in state */}
      {!isCheckedIn && !hasCheckedOut ? (
        /* ========== BEFORE CHECK-IN VIEW ========== */
        <section>
          <div style={{
            textAlign: 'center',
            padding: '40px 24px',
            background: '#fff',
            borderRadius: '12px',
            border: '1px solid #e5e7eb',
            boxShadow: '0 1px 3px rgba(0,0,0,0.08)'
          }}>
            <div style={{
              width: '72px',
              height: '72px',
              margin: '0 auto 20px',
              background: '#dbeafe',
              borderRadius: '50%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '32px',
              color: '#3b82f6'
            }}>
              <FiClock />
            </div>
            <h2 style={{ fontSize: '22px', fontWeight: '600', color: '#111827', marginBottom: '8px' }}>
              Ready to Start Your Shift?
            </h2>
            <p style={{ fontSize: '14px', color: '#6b7280', marginBottom: '28px', maxWidth: '360px', margin: '0 auto 28px' }}>
              Click below to check in and begin your working day.
            </p>
            <button
              onClick={handleCheckIn}
              disabled={actionLoading}
              style={{
                padding: '14px 40px',
                background: actionLoading ? '#9ca3af' : '#111827',
                color: '#fff',
                border: 'none',
                borderRadius: '8px',
                fontSize: '15px',
                fontWeight: '600',
                cursor: actionLoading ? 'not-allowed' : 'pointer',
                transition: 'all 0.2s',
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                margin: '0 auto'
              }}
            >
              {actionLoading ? (
                <>
                  <FiRefreshCw style={{ animation: 'spin 1s linear infinite' }} />
                  Processing...
                </>
              ) : (
                'Check In Now'
              )}
            </button>
          </div>
        </section>
      ) : hasCheckedOut ? (
        /* ========== AFTER CHECK-OUT VIEW ========== */
        <section>
          <div style={{
            textAlign: 'center',
            padding: '40px 24px',
            background: 'linear-gradient(135deg, #10b981 0%, #059669 100%)',
            borderRadius: '12px',
            color: '#fff',
            boxShadow: '0 4px 6px rgba(0,0,0,0.1)',
            marginBottom: '24px'
          }}>
            <h2 style={{ fontSize: '22px', fontWeight: '600', marginBottom: '8px' }}>
              Shift Completed!
            </h2>
            <p style={{ fontSize: '14px', opacity: 0.9, marginBottom: '16px' }}>
              Great work today! You worked for <strong>{calculateHoursWorked()}</strong>
            </p>
            <p style={{ fontSize: '12px', opacity: 0.8 }}>
              Check-in: {formatTime(attendance?.checkInTime)} | Check-out: {formatTime(attendance?.checkOutTime)}
            </p>
          </div>

          {/* Assignments Card */}
          <div style={{
            background: '#fff',
            borderRadius: '12px',
            border: '1px solid #e5e7eb',
            boxShadow: '0 1px 3px rgba(0,0,0,0.08)'
          }}>
            <div style={{
              padding: '14px 16px',
              borderBottom: '1px solid #e5e7eb',
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}>
              <FiPackage style={{ color: '#3b82f6' }} />
              <h2 style={{ fontSize: '16px', fontWeight: '600', margin: 0, color: '#111827' }}>Today's Assignments</h2>
            </div>
            <div style={{ padding: '16px' }}>
              {assignments.length > 0 ? (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                  {assignments.map((assignment) => (
                    <div
                      key={assignment.assignmentId}
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        padding: '12px',
                        background: '#f9fafb',
                        borderRadius: '8px'
                      }}
                    >
                      <span style={{ fontSize: '14px', fontWeight: '500', color: '#111827' }}>{assignment.loadRef}</span>
                      <span style={{
                        fontSize: '12px',
                        fontWeight: '500',
                        padding: '4px 10px',
                        borderRadius: '10px',
                        ...getStatusStyle(assignment.status)
                      }}>
                        {assignment.status}
                      </span>
                    </div>
                  ))}
                </div>
              ) : (
                <p style={{ fontSize: '14px', color: '#9ca3af', textAlign: 'center', padding: '16px' }}>
                  No assignments for today
                </p>
              )}
            </div>
          </div>
        </section>
      ) : (
        /* ========== CHECKED-IN VIEW ========== */
        <>
          {/* Check Out Section */}
          <section style={{ marginBottom: '20px' }}>
            <div style={{
              textAlign: 'center',
              padding: '28px 20px',
              background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
              borderRadius: '12px',
              color: '#fff',
              boxShadow: '0 4px 6px rgba(0,0,0,0.1)'
            }}>
              <h2 style={{ fontSize: '18px', fontWeight: '600', marginBottom: '6px' }}>
                You're Checked In!
              </h2>
              <p style={{ fontSize: '14px', opacity: 0.9, marginBottom: '18px' }}>
                Working for <strong>{calculateHoursWorked()}</strong> today
              </p>
              <button
                onClick={handleCheckOut}
                disabled={actionLoading}
                style={{
                  padding: '12px 36px',
                  background: '#fff',
                  color: '#667eea',
                  border: 'none',
                  borderRadius: '8px',
                  fontSize: '14px',
                  fontWeight: '600',
                  cursor: actionLoading ? 'not-allowed' : 'pointer',
                  transition: 'all 0.2s',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  margin: '0 auto'
                }}
              >
                {actionLoading ? (
                  <>
                    <FiRefreshCw style={{ animation: 'spin 1s linear infinite' }} />
                    Processing...
                  </>
                ) : (
                  'Check Out'
                )}
              </button>
              <p style={{ fontSize: '12px', opacity: 0.8, marginTop: '14px' }}>
                Checked in at: {formatTime(attendance?.checkInTime)}
              </p>
            </div>
          </section>

          {/* Grid Section */}
          <section style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
            gap: '16px'
          }}>
            {/* Assignments Card */}
            <div style={{
              background: '#fff',
              borderRadius: '12px',
              border: '1px solid #e5e7eb',
              boxShadow: '0 1px 3px rgba(0,0,0,0.08)'
            }}>
              <div style={{
                padding: '14px 16px',
                borderBottom: '1px solid #e5e7eb',
                display: 'flex',
                alignItems: 'center',
                gap: '8px'
              }}>
                <FiPackage style={{ color: '#3b82f6' }} />
                <h2 style={{ fontSize: '16px', fontWeight: '600', margin: 0, color: '#111827' }}>Today's Assignments</h2>
              </div>
              <div style={{ padding: '16px' }}>
                {assignments.length > 0 ? (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                    {assignments.map((assignment) => (
                      <div
                        key={assignment.assignmentId}
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                          padding: '12px',
                          background: '#f9fafb',
                          borderRadius: '8px'
                        }}
                      >
                        <span style={{ fontSize: '14px', fontWeight: '500', color: '#111827' }}>{assignment.loadRef}</span>
                        <span style={{
                          fontSize: '12px',
                          fontWeight: '500',
                          padding: '4px 10px',
                          borderRadius: '10px',
                          ...getStatusStyle(assignment.status)
                        }}>
                          {assignment.status}
                        </span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p style={{ fontSize: '14px', color: '#9ca3af', textAlign: 'center', padding: '16px' }}>
                    No assignments for today
                  </p>
                )}
              </div>
            </div>

            {/* Working Hours Card */}
            <div style={{
              background: '#fff',
              borderRadius: '12px',
              border: '1px solid #e5e7eb',
              boxShadow: '0 1px 3px rgba(0,0,0,0.08)'
            }}>
              <div style={{
                padding: '14px 16px',
                borderBottom: '1px solid #e5e7eb',
                display: 'flex',
                alignItems: 'center',
                gap: '8px'
              }}>
                <FiActivity style={{ color: '#f59e0b' }} />
                <h2 style={{ fontSize: '16px', fontWeight: '600', margin: 0, color: '#111827' }}>Working Hours</h2>
              </div>
              <div style={{ padding: '16px' }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                  <div style={{ textAlign: 'center', padding: '14px', background: '#f9fafb', borderRadius: '8px' }}>
                    <p style={{ fontSize: '11px', color: '#6b7280', marginBottom: '4px', textTransform: 'uppercase', fontWeight: '600' }}>
                      Check In
                    </p>
                    <p style={{ fontSize: '16px', fontWeight: '600', color: '#111827', margin: 0 }}>
                      {formatTime(attendance?.checkInTime)}
                    </p>
                  </div>
                  <div style={{ textAlign: 'center', padding: '14px', background: '#f9fafb', borderRadius: '8px' }}>
                    <p style={{ fontSize: '11px', color: '#6b7280', marginBottom: '4px', textTransform: 'uppercase', fontWeight: '600' }}>
                      Check Out
                    </p>
                    <p style={{ fontSize: '16px', fontWeight: '600', color: '#111827', margin: 0 }}>
                      {formatTime(attendance?.checkOutTime)}
                    </p>
                  </div>
                  <div style={{ textAlign: 'center', padding: '14px', background: '#d1fae5', borderRadius: '8px', gridColumn: 'span 2' }}>
                    <p style={{ fontSize: '11px', color: '#065f46', marginBottom: '4px', textTransform: 'uppercase', fontWeight: '600' }}>
                      Hours Worked
                    </p>
                    <p style={{ fontSize: '22px', fontWeight: '600', color: '#047857', margin: 0 }}>
                      {calculateHoursWorked()}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </section>
        </>
      )}

      {/* CSS Animation for spinner */}
      <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  );
}

export default DriverDashboard;
