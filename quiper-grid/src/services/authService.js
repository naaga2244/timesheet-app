// import { users } from "../data/users";
import axios from "axios";

const API_BASE_URL = `${process.env.REACT_APP_API_URL || "http://localhost:7100/api"}/home`;

export const login = async (email) => {

    if (localStorage.getItem("loggedInUser")) {
        return { success: false, message: "A user is already logged in on this browser." };
    }

    try {
        const response = await axios.get(`${API_BASE_URL}/login`, {
            params: { email },
        });

        if (response.data?.success) {
            const user = response.data.data;
            localStorage.setItem("loggedInUser", JSON.stringify(user));
            localStorage.setItem("token", response.data.token);
            return { success: true, message: user };
        } else {
            return {
                success: false,
                message: response.data?.message || "Invalid credentials"
            };
        }

    } catch (error) {
        console.error("Login error:", error);
        return {
            success: false,
            message:
                error.response?.data?.message ||
                "Unable to connect to server. Please try again.",
        };
    }

    // const user = users.find(
    //     (u) => u.email.toLowerCase() === email.toLowerCase()
    // );

    // if (user) {
    //     localStorage.setItem("loggedInUser", JSON.stringify(user));
    //     return user;
    // }

    // return null; // invalid email
};

// Get the logged-in user from localStorage
export const getLoggedInUser = () => {
    const storedUser = localStorage.getItem("loggedInUser");
    return storedUser ? JSON.parse(storedUser) : null;
};

// Logout and clear session
export const logout = () => {
    localStorage.removeItem("loggedInUser");
    localStorage.removeItem("token");
};

export const getToken = () => localStorage.getItem("token");

export const isTokenExpired = (token) => {
    if (!token) return true;

    const payload = token.split('.')[1];
    if (!payload) return true;

    try {
        const decoded = JSON.parse(atob(payload));
        const exp = decoded.exp;
        if (!exp) return true;

        const now = Math.floor(Date.now() / 1000);
        return now >= exp;
    } catch (err) {
        console.error("Error decoding token:", err);
        return true;
    }

}

// Check if user has one of the allowed roles
export const hasAccess = (allowedRoles) => {
    const user = getLoggedInUser();
    return user && allowedRoles.includes(user.role.toLowerCase());
};
