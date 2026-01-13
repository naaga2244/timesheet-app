import React, { useEffect, useState } from "react";
import { getLoggedInUser } from "../../services/authService";
import { adduser, fetchRoles, fetchUsers, updateUser } from "../../services/apiService";
import EditableTable from "../../components/EditableTable";
import PaginationControls from "../../components/PaginationControls";

const UserConfiguration = () => {
    const [userData, setUserData] = useState([]);
    const [roles, setRoles] = useState([]);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(1);
    const loggedInUser = getLoggedInUser();

    const columns = [
        { header: "S.No", accessor: "sno", width: "110px" },
        { header: "User Name", accessor: "name", width: "160px" },
        { header: "Email ID", accessor: "email", width: "" },
        { header: "Role", accessor: "role", width: "120px" },
        { header: "Status", accessor: "status", width: "160px" },
    ];

    const loadRoles = async () => {
        try {
            const res = await fetchRoles();
            if (res?.data) {
                const activeRoles = res.data
                    .filter((r) => r.status?.toLowerCase() === "active")
                    .map((r) => r.role1?.toLowerCase());
                setRoles(activeRoles);
                // setRoles(res.data.map((r) => r.role1?.toLowerCase()));
            }
        } catch (err) {
            console.error("Error fetching roles:", err);
        }
    };

    const loadUsers = async () => {
        try {
            const res = await fetchUsers(page);
            if (res?.success !== false) {
                const filtered =
                    loggedInUser?.role?.toLowerCase() === "admin"
                        ? res.data || []
                        : [];
                setUserData(filtered);
                setTotalPages(res.totalPages || 1);
                setPageSize(res.pageSize || 10);
                setTotalCount(res.totalCount || 0);
            } else {
                setUserData([]);
            }
        } catch (err) {
            console.error("Error loading users:", err);
        }
    }

    useEffect(() => {
        loadRoles();
        // eslint-disable-next-line
    }, []);

    useEffect(() => {
        loadUsers();
        // eslint-disable-next-line
    }, [page]);

    return (
        <div className="container">

            <h4 className="text-center">User Configuration Page</h4>

            <EditableTable
                columns={columns}
                data={userData}
                setData={setUserData}
                canEdit={true}
                showAddButton={true}
                showDelete={false}
                onDataChange={loadUsers}
                addItem={adduser}
                updateItem={updateUser}
                dropdownOptions={{
                    role: roles,
                }}
                currentPage = {page}
                pageSize={pageSize}
                totalCount={totalCount}
            />

            <PaginationControls
                page={page}
                totalPages={totalPages}
                onPageChange={setPage}
            />

            {/* <div className="d-flex justify-content-around align-items-center mt-5">
                <button
                    className="btn btn-outline-dark me-2"
                    disabled={page <= 1}
                    onClick={() => setPage((p) => p - 1)}
                >
                    Previous
                </button>
                <span>
                    Page {page} of {totalPages}
                </span>
                <button
                    className="btn btn-outline-dark ms-2"
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => p + 1)}
                >
                    Next
                </button>
            </div> */}
        </div>
    );
};

export default UserConfiguration;
