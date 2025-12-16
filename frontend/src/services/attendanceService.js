import api from  "./api";

export const getAttendance = () => api.get("/admin/attendance");
export const getAttendanceStats = () => api.get("/admin/attendance/stats");
