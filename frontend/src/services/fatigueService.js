import api from './api';

// Get fatigue breakdown for a specific driver (admin)
export const getDriverFatigue = (driverId) => api.get(`/fatigue/driver/${driverId}`);

// Recalculate fatigue score for a driver (admin)
export const calculateDriverFatigue = (driverId) => api.post(`/fatigue/calculate/${driverId}`);

// Recalculate fatigue scores for all drivers (admin)
export const calculateAllFatigue = () => api.post('/fatigue/calculate-all');

// Get fatigue summary statistics (admin dashboard)
export const getFatigueSummary = () => api.get('/fatigue/summary');

// Get all drivers with fatigue details (admin)
export const getAllDriversFatigue = () => api.get('/fatigue/drivers');

// Get fatigue alerts for high fatigue drivers (admin)
export const getFatigueAlerts = () => api.get('/fatigue/alerts');

// Driver endpoints
// Get current driver's fatigue breakdown
export const getMyFatigue = () => api.get('/driver/fatigue');

// Get current driver's workload
export const getMyWorkload = () => api.get('/driver/workload');
