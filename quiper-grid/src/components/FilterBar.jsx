import React from "react";
import { OverlayTrigger, Tooltip } from "react-bootstrap";
// import { FaRedo } from "react-icons/fa";
// import { BsBootstrapReboot } from "react-icons/bs";
const FilterBar = ({
    users = [],
    clients = [],
    startDate,
    endDate,
    selectedClient,
    selectedUser,
    onChange,
    onUserChange,
    onClientChange,
    onReset,
    showUser = true,
    showClient = true,
}) => {
    return (
        <div className="container-fluid bg-white px-4 py-2 shadow-sm rounded mb-2">
            <div className="row align-items-center justify-content-center"
                style={{ gap: "4rem" }}
            >

                {/* Start Date */}
                <div className="col-auto">
                    <div className="input-group input-group-sm">
                        <span className="input-group-text">Start Date</span>
                        <input type="date" className="form-control"
                            value={startDate || ""}
                            onChange={e => onChange({ startDate: e.target.value })}
                        />
                    </div>
                </div>

                {/* End Date */}
                <div className="col-auto">
                    <div className="input-group input-group-sm">
                        <span className="input-group-text">End Date</span>
                        <input type="date" className="form-control"
                            value={endDate}
                            onChange={e => onChange({ startDate, endDate: e.target.value })}
                        />
                    </div>
                </div>
                {/* User Dropdown */}
                {showUser && (
                    <div className="col-auto">
                        <div className="input-group input-group-sm">
                            <span className="input-group-text">User</span>
                            <select className="form-select"
                                value={selectedUser || ""}
                                onChange={e => onUserChange(e.target.value)}
                            >
                                <option value="">Select User</option>
                                {users.map((user, idx) => (
                                    <option key={idx}>{user}</option>
                                ))}
                            </select>
                        </div>
                    </div>
                )}

                {/* Client Dropdown */}
                {showClient && (
                    <div className="col-auto">
                        <div className="input-group input-group-sm">
                            <span className="input-group-text">Client</span>
                            <select className="form-select"
                                value={selectedClient || ""}
                                onChange={e => onClientChange(e.target.value)}
                            >
                                <option value="">Select Client</option>
                                {clients.map((client, idx) => (
                                    <option key={idx}>{client}</option>
                                ))}
                            </select>
                        </div>
                    </div>
                )}

                {/* R Button */}
                <div className="col-auto">
                    <OverlayTrigger
                        placement="top-start"
                        overlay={<Tooltip id="reset-tooltip">Clear Filters</Tooltip>}
                    >
                        <button className="btn btn-md btn-outline-primary btn-sm"
                            onClick={onReset}
                            type="button"
                            style={{
                                // padding: "2px 10px",
                                minWidth: "80px",
                                fontSize: "14px",
                                fontWeight: "500",
                                whiteSpace: "nowrap"
                            }}
                        >
                            {/* <FaRedo size={14} /> */}
                            Reset Filters
                        </button>
                    </OverlayTrigger>
                </div>

            </div>
        </div>
    );
};

export default FilterBar;