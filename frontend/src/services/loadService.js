import api from "./api";

export const getAllLoads = (filters) => api.get("/loads", { params: filters });
export const getLoadById = (id) => api.get(`/loads/${id}`);
export const createLoad = (data) => api.post("/loads", data);
export const updateLoad = (id, data) => api.put(`/loads/${id}`, data);
export const deleteLoad = (id) => api.delete(`/loads/${id}`);
export const getLoadStats = () => api.get("/loads/stats");
