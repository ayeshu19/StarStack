import api from "./api";

// Get attendance report
export const getAttendanceReport = (params = {}) => {
  return api.get("/admin/reports/attendance", { params });
};

// Get workload report
export const getWorkloadReport = (params = {}) => {
  return api.get("/admin/reports/workload", { params });
};

// Get fatigue trends report
export const getFatigueTrendsReport = (params = {}) => {
  return api.get("/admin/reports/fatigue-trends", { params });
};

// Get work hours report
export const getWorkHoursReport = (params = {}) => {
  return api.get("/admin/reports/work-hours", { params });
};

// Get individual driver report
export const getDriverReport = (driverId, params = {}) => {
  return api.get(`/admin/reports/driver/${driverId}`, { params });
};

// Export report as CSV (client-side)
export const exportToCSV = (data, filename) => {
  if (!data || data.length === 0) return;

  const headers = Object.keys(data[0]);
  const csvContent = [
    headers.join(","),
    ...data.map(row =>
      headers.map(header => {
        const value = row[header];
        // Escape commas and quotes
        if (typeof value === "string" && (value.includes(",") || value.includes('"'))) {
          return `"${value.replace(/"/g, '""')}"`;
        }
        return value ?? "";
      }).join(",")
    )
  ].join("\n");

  const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `${filename}_${new Date().toISOString().split("T")[0]}.csv`;
  link.click();
};
