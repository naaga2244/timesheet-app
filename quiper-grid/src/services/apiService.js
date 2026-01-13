import axios from "axios";
import { getToken } from "./authService";

axios.interceptors.request.use(
    (config) => {
        const token = getToken();
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);
const API_BASE_URL = process.env.REACT_APP_API_URL || "http://localhost:7100/api";

export const downloadReportTemplate = async (username) => {
    const response = await axios.get(`${API_BASE_URL}/Home/report-template`, {
        params: { username },
        responseType: "blob",
    });
    return response; // caller handles blob download
};


//Role API
export const updateRole = async (id, updatedData) => {
    try {
        const response = await axios.put(`${API_BASE_URL}/Admin/update-role/${id}`, updatedData);
        return response.data;
    } catch (error) {
        console.error("Error updating role:", error);
        return { success: false, message: "Failed to update role" };
    }
};

export const fetchRoles = async (pagenumber = 1, pageSize = 10) => {
    try {
        const response = await axios.get(`${API_BASE_URL}/Home/fetch-roles`, {
            params: { pagenumber, pageSize },
        });

        return response.data;

    } catch (error) {
        console.error("Error fetching reports:", error);
        return { success: false, data: [], totalPages: 1 };
    }
};

export const addRole = async (roleData) => {
    try {
        const response = await axios.post(`${API_BASE_URL}/Admin/add-role`, roleData);
        return response.data;
    } catch (error) {
        console.error("Error adding role:", error);
        return { success: false, message: "Failed to add role" };
    }
};

//Report API
export const fetchReports = async (pagenumber = 1, pageSize = 10, startDate,
    endDate, username, client, sortColumn,
    sortDirection) => {
    try {
        const params = { pagenumber, pageSize, sortColumn, sortDirection };
        if (username) params.username = username;
        if (client) params.client = client;
        if (startDate) params.startDate = startDate;
        if (endDate) params.endDate = endDate;
        if (sortColumn) params.sortColumn = sortColumn;
        if (sortDirection) params.sortDirection = sortDirection;

        const response = await axios.get(`${API_BASE_URL}/Admin/fetch-reports`, {
            params
        });
        return response.data;

    } catch (error) {
        console.error("Error fetching reports:", error);
        return { success: false, data: [], totalPages: 1 };
    }
};


export const fetchUserReports = async (username, pagenumber = 1, pageSize = 10,
    startDate, endDate, sortDirection, sortColumn) => {
    try {
        const params = { username, pagenumber, pageSize, sortColumn, sortDirection };
        if (startDate) params.startDate = startDate;
        if (endDate) params.endDate = endDate;
        if (sortColumn) params.sortColumn = sortColumn;
        if (sortDirection) params.sortDirection = sortDirection;

        const response = await axios.get(`${API_BASE_URL}/User/userreports`, {
            params
        });
        return response.data;

    } catch (error) {
        console.error("Error fetching reports:", error);
        return { success: false, data: [], totalPages: 1 };
    }
};

export const addReport = async (reportData) => {
    try {
        const response = await axios.post(`${API_BASE_URL}/Home/add-report`, reportData);
        return response.data;
    } catch (error) {
        console.error("Error adding report:", error);
        return { success: false, message: error.message };
    }
};

export const updateReport = async (id, updateData) => {
    try {
        const response = await axios.put(
            `${API_BASE_URL}/Home/update-report/${id}`, updateData);
        return response.data;

    } catch (error) {
        console.error("Error updating reports:", error);
        return { success: false, message: error };
    }
};

export const deleteReport = async (id) => {
    try {
        const response = await axios.delete(
            `${API_BASE_URL}/Home/delete-report/${id}`
        );
        return response.data;
    } catch (error) {
        console.error("Error deleting report:", error);
        return { success: false, message: error };
    }
};

//Project API
export const fetchProjects = async (pagenumber = 1, pageSize = 10) => {
    try {
        const response = await axios.get(`${API_BASE_URL}/Home/fetch-projects`, {
            params: { pagenumber, pageSize },
        });

        return response.data;

    } catch (error) {
        console.error("Error fetching reports:", error);
        return { success: false, data: [], totalPages: 1 };
    }
};

export const addProject = async (projectData) => {
    try {
        const response = await axios.post(`${API_BASE_URL}/Admin/add-project`, projectData);
        return response.data;
    } catch (error) {
        console.error("Error adding report:", error);
        return { success: false, message: error.message };
    }
};

export const updateProject = async (id, updatedData) => {
    try {
        const response = await axios.put(`${API_BASE_URL}/Admin/update-project/${id}`, updatedData);
        return response.data;
    } catch (error) {
        console.error("Error updating role:", error);
        return { success: false, message: "Failed to update role" };
    }
};

//Client API
export const updateClient = async (id, updatedData) => {
    try {
        const response = await axios.put(`${API_BASE_URL}/Admin/update-client/${id}`, updatedData);
        return response.data;
    } catch (error) {
        console.error("Error updating role:", error);
        return { success: false, message: "Failed to update role" };
    }
};

export const fetchClients = async (pagenumber = 1, pageSize = 10) => {
    try {
        const response = await axios.get(`${API_BASE_URL}/Home/fetch-clients`, {
            params: { pagenumber, pageSize },
        });

        return response.data;

    } catch (error) {
        console.error("Error fetching reports:", error);
        return { success: false, data: [], totalPages: 1 };
    }
};

export const addClient = async (clientData) => {
    try {
        const response = await axios.post(`${API_BASE_URL}/Admin/add-client`, clientData);
        return response.data;
    } catch (error) {
        console.error("Error adding role:", error);
        return { success: false, message: "Failed to add role" };
    }
};

//User API
export const updateUser = async (id, updatedData) => {
    try {
        const response = await axios.put(`${API_BASE_URL}/Admin/update-user/${id}`, updatedData);
        return response.data;
    } catch (error) {
        console.error("Error updating user:", error);
        return {
            success: false,
            message: error.response?.data?.message || "Failed to add user"
        };
    }
};

export const adduser = async (userData) => {
    try {
        const response = await axios.post(`${API_BASE_URL}/Admin/add-user`, userData);
        return response.data;
    } catch (error) {
        console.error("Error adding user:", error);
        return {
            success: false,
            message: error.response?.data?.message || "Failed to add user"
        };
    }
};

export const fetchUsers = async (pagenumber = 1, pageSize = 10) => {
    try {
        const response = await axios.get(`${API_BASE_URL}/Home/fetch-users`, {
            params: { pagenumber, pageSize },
        });

        return response.data;

    } catch (error) {
        console.error("Error fetching reports:", error);
        return { success: false, data: [], totalPages: 1 };
    }
};
