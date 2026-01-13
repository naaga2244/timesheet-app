import React, { useEffect, useState } from "react";
import EditableTable from "../../components/EditableTable";
import { addRole, fetchRoles, updateRole } from "../../services/apiService";
import { getLoggedInUser } from "../../services/authService";
import PaginationControls from "../../components/PaginationControls";

const RoleConfiguration = () => {
    const [roleData, setRoleData] = useState([]);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [totalCount, setTotalCount] = useState(0);
    const loggedInUser = getLoggedInUser();
    const columns = [
        { header: "SNo", accessor: "sno", width: "100px" },
        { header: "Role", accessor: "role1" },
        { header: "Status", accessor: "status", width: "160px" }
    ];

    const loadRoles = async () => {
        try {
            const res = await fetchRoles(page);
            const filtered = loggedInUser?.role === "admin"
                ? res.data
                : "";
            setRoleData(filtered);
            setTotalPages(res.totalPages || 1);
            setPageSize(res.pageSize || 10);
            setTotalCount(res.totalCount || 0);
        } catch (err) {
            console.error("Error loading reports:", err);
        }
    };

    useEffect(() => {
        loadRoles();
        // eslint-disable-next-line
    }, [page]);

    return (
        <div className="container table-wrapper">
            <h4 className="text-center">Role Configuration Page</h4>
            <EditableTable
                columns={columns}
                data={roleData}
                setData={setRoleData}
                canEdit={true}
                showAddButton={true}
                showDelete={false}
                onDataChange={loadRoles}
                addItem={addRole}
                updateItem={updateRole}
                currentPage = {page}
                pageSize={pageSize}
                totalCount={totalCount}
            />

            <PaginationControls
                page={page}
                totalPages={totalPages}
                onPageChange={setPage}
            />

            {/* <div className="d-flex justify-content-around align-items-center mt-5 w-100">
                <button
                    className="btn btn-outline-dark me-2"
                    disabled={page <= 1}
                    onClick={() => setPage(page - 1)}
                >
                    Previous
                </button>
                <span>
                    Page {page} of {totalPages}
                </span>
                <button
                    className="btn btn-outline-dark ms-2"
                    disabled={page >= totalPages}
                    onClick={() => setPage(page + 1)}
                >
                    Next
                </button>
            </div> */}
        </div>
    );
};
export default RoleConfiguration;