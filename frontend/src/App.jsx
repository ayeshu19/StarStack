import React from "react";
import { Routes, Route, Navigate, useLocation } from "react-router-dom";

import Sidebar from "./components/Sidebar";
import AdminDashboard from "./pages/AdminDashboard";
import DriverDashboard from "./pages/DriverDashboard";
import DriverManagement from "./pages/DriverManagement";
import Attendance from "./pages/Attendance";
import Reports from "./pages/Reports";
import LoadManagement from "./pages/LoadManagement";
import AutoAssignment from "./pages/AutoAssignment";
import FatigueSafety from "./pages/FatigueAndSafety";
import SignIn from "./pages/Signin";
import ResetPassword from "./pages/ResetPassword";
import ProtectedRoute from "./components/ProtectedRoute";

function App() {
  const location = useLocation();
  const hideSidebar = location.pathname === "/" || location.pathname === "/driver-dashboard" || location.pathname === "/driver/reset-password";

  return (
    <div className="layout">
      {!hideSidebar && <Sidebar />}

      <main className="main">
        <Routes>
          <Route path="/" element={<SignIn />} />

          <Route path="/dashboard" element={<Navigate to="/admin" replace />} />

          {/* Admin Routes */}
          <Route path="/admin" element={<AdminDashboard />} />
          <Route path="/drivers" element={<DriverManagement />} />
          <Route path="/attendance" element={<Attendance />} />
          <Route path="/reports" element={<Reports />} />
          <Route path="/loads" element={<LoadManagement />} />
          <Route path="/auto-assignment" element={<AutoAssignment />} />
          <Route path="/fatigue" element={<FatigueSafety />} />

          {/* Driver Route */}
          <Route path="/driver-dashboard" element={<ProtectedRoute roleRequired="DRIVER"><DriverDashboard /></ProtectedRoute>} />

          <Route path="*" element={<div>404 - Page not found</div>} />
          <Route path="/driver/reset-password" element={<ResetPassword />} />

        </Routes>
      </main>
    </div>
  );
}

export default App;