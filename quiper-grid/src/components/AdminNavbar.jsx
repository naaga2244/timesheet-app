import React from "react";
import companyLogo from "../assets/Timetric.png";
import { getLoggedInUser, logout } from "../services/authService";
import { Dropdown } from "react-bootstrap";

const AdminNavbar = () => {
    const loggedInUser = getLoggedInUser();
    const handleLogout = () => {
        logout();
        window.location.href = "/";
    };

    const isAdmin = loggedInUser?.role?.toLowerCase() === "admin";

    return (
        <nav className="navbar navbar-expand-lg bg-white shadow-sm mb-2 w-100 border-bottom">
            <div className="container-fluid">
                {/* Company Logo */}
                <a className="navbar-brand" href=".">
                    <img
                        src={companyLogo}
                        alt="Company Logo"
                        style={{ width: "120px", height: "auto" }}
                    />
                </a>

                {isAdmin && (
                    <>
                        <button
                            className="navbar-toggler"
                            type="button"
                            data-bs-toggle="collapse"
                            data-bs-target="#navbarNav"
                            aria-controls="navbarNav"
                            aria-expanded="false"
                            aria-label="Toggle navigation"
                        >
                            <span className="navbar-toggler-icon"></span>
                        </button>

                        {/* Navbar Links */}
                        <div className="collapse navbar-collapse" id="navbarNav">
                            <ul className="navbar-nav me-auto align-items-center" style={{ gap: "1rem" }}>
                                {/* <li className="nav-item dropdown">
                                    <a
                                        className="nav-link dropdown-toggle text-black"
                                        href="."
                                        id="configDropdown"
                                        role="button"
                                        data-bs-toggle="dropdown"
                                        aria-expanded="false"
                                    >
                                        Config
                                    </a>

                                    <ul className="dropdown-menu dropdown-menu-white" aria-labelledby="configDropdown">
                                        <li><a className="dropdown-item" href="/admin/config/clients">Client Config</a></li>
                                        <li><a className="dropdown-item" href="/admin/config/projects">Project Config</a></li>
                                        <li><a className="dropdown-item" href="/admin/config/roles">Role Config</a></li>
                                        <li><a className="dropdown-item" href="/admin/config/users">User Config</a></li>
                                    </ul>
                                </li> */}

                                <li className="nav-item">
                                    <Dropdown>
                                        <Dropdown.Toggle
                                            variant="white"
                                            className="text-black fw-normal border-0"
                                            id="config-dropdown"
                                        >
                                            Config
                                        </Dropdown.Toggle>

                                        <Dropdown.Menu>
                                            <Dropdown.Item href="/admin/config/clients">
                                                Client Config
                                            </Dropdown.Item>
                                            <Dropdown.Item href="/admin/config/projects">
                                                Project Config
                                            </Dropdown.Item>
                                            <Dropdown.Item href="/admin/config/roles">
                                                Role Config
                                            </Dropdown.Item>
                                            <Dropdown.Item href="/admin/config/users">
                                                User Config
                                            </Dropdown.Item>
                                        </Dropdown.Menu>
                                    </Dropdown>
                                </li>

                                <li className="nav-item">
                                    <a className="nav-link text-black" href="/admin">Update Timesheet</a>
                                </li>

                                <li className="nav-item">
                                    <a className="nav-link text-black" href="/admin/view-timesheet">View Timesheet</a>
                                </li>

                            </ul>
                        </div>
                    </>
                )}
                <div className="d-flex align-items-center">
                    <span className="me-3 text-black fw-semibold">
                        {loggedInUser ? loggedInUser.name : ""}
                    </span>
                    <button
                        className="btn btn-danger btn-sm"
                        onClick={handleLogout}
                    >
                        Logout
                    </button>
                </div>
            </div>
        </nav>
        // </div>
    );
};

export default AdminNavbar;