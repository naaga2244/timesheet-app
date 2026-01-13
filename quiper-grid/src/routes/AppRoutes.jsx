import React from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
// import { useOutletContext } from "react-router-dom";
import AdminTable from "../components/AdminTable";
import LoginPage from "../pages/LoginPage";
import AdminPage from "../pages/AdminPage";
import UserPage from "../pages/UserPage";
import ProtectedRoute from "../components/ProtectedRoute";
import ClientConfiguration from "../pages/configuration/ClientConfiguration";
import ProjectConfiguration from "../pages/configuration/ProjectConfiguration";
import RoleConfiguration from "../pages/configuration/RoleConfuguration";
import UserConfiguration from "../pages/configuration/UserConfiguration";

const AppRoutes = () => (
    <BrowserRouter>
        <Routes>
            <Route path="/" element={<LoginPage />} />
            <Route
                path="/admin"
                element={
                    <ProtectedRoute allowedRoles={["admin"]} >
                        <AdminPage />
                    </ProtectedRoute>
                }
            >
                <Route index element={<div />} />
                <Route path="view-timesheet" element={<AdminTable />} />
                <Route path="config">
                    <Route path="clients" element={<ClientConfiguration />} />
                    <Route path="projects" element={<ProjectConfiguration />} />
                    <Route path="roles" element={<RoleConfiguration />} />
                    <Route path="users" element={<UserConfiguration />} />
                </Route>
            </Route>
            <Route
                path="/user"
                element={
                    <ProtectedRoute allowedRoles={["user"]} >
                        <UserPage />
                    </ProtectedRoute>
                }
            />
        </Routes>
    </BrowserRouter>
);

export default AppRoutes;