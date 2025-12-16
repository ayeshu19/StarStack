// src/services/adminDriverService.js
import api from './api';

export const getAllDrivers = (filters) => {
  return api.get('/admin/drivers', { params: filters });
};

export const getDriverById = (id) => {
  return api.get(`/admin/drivers/${id}`);
};

export const createDriver = (data) => {
  return api.post('/admin/drivers', data);
};

export const updateDriver = (id, data) => {
  return api.put(`/admin/drivers/${id}`, data);
};

export const updateDriverStatus = (id, status) => {
  return api.patch(`/admin/drivers/${id}/status`, { status });
};

export const getDriverStats = () => {
  return api.get('/admin/drivers/stats');
};

export const uploadDriverCsv = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/admin/drivers/upload-csv', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

export const getRegions = () => {
  return api.get('/admin/drivers/regions');
};

export const getVehicleTypes = () => {
  return api.get('/admin/drivers/vehicle-types');
};

export const getWeeklyOffs = () => {
  return api.get('/admin/drivers/weekly-offs');
};