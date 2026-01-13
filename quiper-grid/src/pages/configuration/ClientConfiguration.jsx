import React, { useEffect, useState } from "react";
import { getLoggedInUser } from "../../services/authService";
import { addClient, fetchClients, updateClient } from "../../services/apiService";
import EditableTable from "../../components/EditableTable";
import PaginationControls from "../../components/PaginationControls";

const ClientConfiguration = () => {
    const [clientData, setClientData] = useState([]);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(1);
    const loggedInUser = getLoggedInUser();


    const columns = [
        { header: "SNo", accessor: "sno", width: "100px" },
        { header: "Client Name", accessor: "name" },
        { header: "Status", accessor: "status", width: "160px" },
    ];

    const loadClients = async () => {
        try {
            const res = await fetchClients(page);
            const filtered = loggedInUser?.role === "admin" ? res.data : "";
            setClientData(filtered);
            setTotalPages(res.totalPages || 1);
            setPageSize(res.pageSize || 10);
            setTotalCount(res.totalCount || 0);
        } catch (err) {
            console.error("Error loading clients:", err);
        }
    };

    useEffect(() => {
        loadClients();
        // eslint-disable-next-line
    }, [page]);

    return (
        <div className="container table-wrapper">
            <h4 className="text-center">Client Configuration Page</h4>
            <EditableTable
                columns={columns}
                data={clientData}
                setData={setClientData}
                canEdit={true}
                showAddButton={true}
                showDelete={false}
                onDataChange={loadClients}
                addItem={addClient}
                updateItem={updateClient}
                currentPage={page}
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

export default ClientConfiguration;