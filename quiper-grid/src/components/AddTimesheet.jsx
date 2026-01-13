import React, { useEffect, useState } from "react";
import EditableTable from "./EditableTable";
import { updateReport, deleteReport, addReport, fetchReports, fetchClients, fetchProjects } from "../services/apiService";
import FilterBar from "../components/FilterBar";
import { getLoggedInUser } from "../services/authService";
import ExcelJS from "exceljs";
import fs from "file-saver";
import PaginationControls from "./PaginationControls";

const AddTimesheet = () => {
    const loggedInUser = getLoggedInUser();
    const [reportData, setReportData] = useState([]);
    const [clients, setClients] = useState([]);
    const [projects, setProjects] = useState([]);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [startDate, setStartDate] = useState(new Date().toISOString().split("T")[0]);
    const [endDate, setEndDate] = useState(new Date().toISOString().split("T")[0]);
    const [selectedClient] = useState("");
    const [sortColumn, setSortColumn] = useState("Date");
    const [sortDirection, setSortDirection] = useState("asc");
    const capitalizeFirstLetter = (str) => str.charAt(0).toUpperCase() + str.slice(1);


    const loadReports1 = async () => {
        try {
            const apiSortColumn = capitalizeFirstLetter(sortColumn);
            const res = await fetchReports(
                page,
                10,
                startDate,
                endDate,
                loggedInUser.name,
                selectedClient,
                apiSortColumn,
                sortDirection
            );
            const filtered =
                loggedInUser?.role === "admin"
                    ? res.data
                    : "";
            setReportData(filtered);
            setTotalPages(res.totalPages || 1);
        } catch (err) {
            console.error("Error loading reports:", err);
        }
    };

    const loadDropdownData = async () => {
        try {
            const [clientRes, projectRes] = await Promise.all([
                fetchClients(),
                fetchProjects()
            ]);

            setClients(clientRes.data || []);
            setProjects(projectRes.data || []);
        } catch (err) {
            console.error("Error loading dropdown data:", err);
        }
    };


    const formatDate = (dateString) => {
        if (!dateString) return "";
        const [year, month, day] = dateString.split("-");
        return `${day}-${month}-${year}`;
    };

    const handleExportUserReports = async () => {
        try {
            let allData = [];
            let currentPage = 1;
            const pageSize = 10;
            let totalPages = 1;

            do {
                const res = await fetchReports(
                    currentPage,
                    pageSize,
                    startDate,
                    endDate,
                    loggedInUser.name,    // 👈 FIXED user export
                    selectedClient,
                    capitalizeFirstLetter(sortColumn),
                    sortDirection
                );

                if (!res?.success) break;

                allData = allData.concat(res.data || []);
                totalPages = res.totalPages || 1;
                currentPage++;
            } while (currentPage <= totalPages);

            if (!allData.length) {
                alert("No reports to export");
                return;
            }

            // Create workbook
            const workbook = new ExcelJS.Workbook();
            const sheetName = `${loggedInUser.name}Timesheet`;
            const worksheet = workbook.addWorksheet(sheetName);

            // Convert date for export
            const exportRows = allData.map((r, i) => ({
                "S.No": i + 1,
                Date: formatDate(r.date),
                Username: r.username ?? "",
                Client: r.client ?? "",
                Project: r.project ?? "",
                Task: r.task ?? "",
                "Ticket No.": r.ticket ?? "",
                Call: r.call ?? "",
                "Man Hours": r.manHours !== undefined ? parseInt(r.manHours) : 0,
                "Man Minutes": r.manMinutes !== undefined ? parseInt(r.manMinutes) : 0
            }));

            const headers = Object.keys(exportRows[0]);
            const headerRow = worksheet.addRow(headers);

            headerRow.eachCell((cell) => {
                cell.font = { bold: true };
                cell.alignment = { horizontal: "center", vertical: "middle" };
                cell.fill = { type: "pattern", pattern: "solid", fgColor: { argb: "FFCCCCCC" } };
                cell.border = {
                    top: { style: "medium" },
                    left: { style: "medium" },
                    bottom: { style: "medium" },
                    right: { style: "medium" }
                };
            });

            exportRows.forEach((row) => {
                const dataRow = worksheet.addRow(Object.values(row));
                dataRow.eachCell((cell) => {
                    cell.border = {
                        top: { style: "thin" },
                        left: { style: "thin" },
                        bottom: { style: "thin" },
                        right: { style: "thin" }
                    };
                });
            });

            worksheet.columns.forEach((column) => {
                let maxLength = 0;
                column.eachCell({ includeEmpty: true }, (cell) => {
                    const value = cell.value ? cell.value.toString() : "";
                    if (value.length > maxLength) maxLength = value.length;
                });
                column.width = maxLength + 5;
            });

            const buffer = await workbook.xlsx.writeBuffer();
            const blob = new Blob([buffer], { type: "application/octet-stream" });
            fs.saveAs(blob, `${sheetName}.xlsx`);

        } catch (err) {
            console.error("Error exporting user reports:", err);
            alert("Failed to export user reports. Check console.");
        }
    };



    const columns = [
        { header: "Date", accessor: "date", sortable: true, width: "140px" },
        { header: "Username", accessor: "username", width: "140px" },
        { header: "Client", accessor: "client", width: "120px" },
        { header: "Project / Application", accessor: "project", width: "166px"},
        { header: "Task", accessor: "task" },
        { header: "Ticket No.", accessor: "ticket", width: "110px" },
        { header: "Call", accessor: "call", width: "110px" },
        { header: "Man Hours", accessor: "manHours", width: "110px" },
        { header: "Man Minutes", accessor: "manMinutes", width: "110px" },
    ];

    useEffect(() => {
        loadReports1();
        loadDropdownData();
        // eslint-disable-next-line
    }, [startDate, endDate, sortDirection, sortColumn, page]);

    const dropdownOptions1 = {
        client: clients.filter((c) => c.status?.toLowerCase() !== "inactive").map((c) => c.name),
        project: projects.filter((p) => p.status?.toLowerCase() !== "inactive").map((p) => p.project1),
        call: ["Yes", "No"],
        manHours: ["0", "1", "2"],
        manMinutes: ["0", "15", "30", "45"],
    };

    return (
        <>
            <FilterBar
                showUser={false}
                showClient={false}
                startDate={startDate}
                endDate={endDate}
                // selectedUser={selectedUser}
                // selectedClient={selectedClient}

                onChange={({ startDate: s, endDate: e }) => {
                    setStartDate(s);
                    setEndDate(e);
                    setPage(1);
                    // loadReports(s, e);
                }}

                // onUserChange={(user) => {
                //     setSelectedUser(user);
                //     // loadReports(user);
                //     onPageChange(1);
                // }}

                // onClientChange={(client) => {
                //     setSelectedClient(client);
                //     // loadReports(client);
                //     onPageChange(1);
                // }
                // }

                onReset={() => {
                    const today = new Date().toISOString().split("T")[0];
                    setStartDate(today);
                    setEndDate(today);
                    setSortDirection("asc");
                    // setSelectedUser(loggedInUser.name);
                    // setSelectedClient("");
                    // loadReports();
                    setPage(1);
                }}
            />
            <div className="container-fluid px-4 table-wrapper">
                <EditableTable
                    columns={columns}
                    data={reportData}
                    setData={setReportData}
                    dropdownOptions={dropdownOptions1}
                    canEdit={true}
                    showAddButton={true}
                    readOnlyFields={["username"]}
                    defaultUsername={loggedInUser.name}
                    Isrole={loggedInUser.role ==="admin" ? "admin" : ""}
                    onDataChange={() => loadReports1()}
                    addItem={addReport}
                    updateItem={updateReport}
                    deleteItem={deleteReport}
                    setSortColumn={setSortColumn}
                    sortColumn={sortColumn}
                    sortDirection={sortDirection}
                    setSortDirection={setSortDirection}
                    loadReports={loadReports1}
                    onPageChange={(newPage) => setPage(newPage)}
                    onExportExcel={handleExportUserReports}
                    showExportButton={true}
                    showHeader={true}
                />

                <PaginationControls
                    page={page}
                    totalPages={totalPages}
                    onPageChange={setPage}
                />

                {/* <div className="d-flex justify-content-around align-items-center mt-4">
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
        </>
    );
};

export default AddTimesheet;