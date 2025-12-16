import api from "./api";

// Get driver dashboard data
export const getDriverDashboard = async () => {
  const response = await api.get("/driver/dashboard");
  return response.data;
};

// Check in driver
export const checkInDriver = async () => {
  const response = await api.post("/driver/checkin");
  return response.data;
};

// Check out driver
export const checkOutDriver = async () => {
  const response = await api.post("/driver/checkout");
  return response.data;
};

// Get today's attendance
export const getTodayAttendance = async () => {
  const response = await api.get("/driver/attendance/today");
  return response.data;
};

// Get driver profile
export const getDriverProfile = async () => {
  const response = await api.get("/driver/me");
  return response.data;
};

// Get driver's fatigue breakdown
export const getDriverFatigue = async () => {
  const response = await api.get("/driver/fatigue");
  return response.data;
};

// Get driver's current workload
export const getDriverWorkload = async () => {
  const response = await api.get("/driver/workload");
  return response.data;
};
