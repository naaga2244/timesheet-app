import React, { useEffect, useState } from "react";
import { Navigate } from "react-router-dom";
import { getLoggedInUser, getToken, logout } from "../services/authService";

const ProtectedRoute = ({ children, allowedRoles }) => {
    const [loggedOut, setLoggedOut] = useState(false);
    const user = getLoggedInUser();
    const token = getToken();

    useEffect(() => {
        if (!token) return;

        try {
            // Decode JWT payload
            const payload = JSON.parse(atob(token.split('.')[1]));
            const exp = payload.exp; // expiry in seconds
            const now = Math.floor(Date.now() / 1000);
            const timeUntilExpiry = (exp - now) * 1000; // in ms

            if (timeUntilExpiry <= 0) {
                // Already expired
                logout();
                setLoggedOut(true);
                return;
            }

            // Set auto logout when token expires
            const timer = setTimeout(() => {
                logout();
                setLoggedOut(true);
            }, timeUntilExpiry);

            return () => clearTimeout(timer); // cleanup on unmount
        } catch (err) {
            console.error("Error decoding token:", err);
            logout();
            setLoggedOut(true);
        }
    }, [token]);

    // If no one is logged in, redirect to login
    if (!user || !token || loggedOut) {
        // logout();
        return <Navigate to="/" replace />;
    }

    // If the logged-in user's role is not allowed, redirect to login
    if (!allowedRoles.includes(user.role.toLowerCase())) {
        // logout();
        return <Navigate to="/" replace />;
    }


    return children;
};

export default ProtectedRoute;