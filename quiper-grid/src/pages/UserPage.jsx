import React, { useEffect, useState } from "react";
import AdminNavbar from "../components/AdminNavbar";
import EditableTable from "../components/EditableTable";
import { getLoggedInUser } from "../services/authService";
import { fetchClients, fetchProjects, fetchUserReports, downloadReportTemplate } from "../services/apiService";
import { updateReport, deleteReport, addReport } from "../services/apiService";
import FilterBar from "../components/FilterBar";
import ExcelJS from "exceljs";
import fs from "file-saver";
import PaginationControls from "../components/PaginationControls";


const UserPage = () => {
    const loggedInUser = getLoggedInUser();
    const [clients, setClients] = useState([]);
    const [projects, setProjects] = useState([]);
    const [reportData, setReportData] = useState([]);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [startDate, setStartDate] = useState(new Date().toISOString().split("T")[0]);
    const [endDate, setEndDate] = useState(new Date().toISOString().split("T")[0]);
    const [sortColumn, setSortColumn] = useState("Date"); // default column
    const [sortDirection, setSortDirection] = useState("asc"); // default direction
    const capitalizeFirstLetter = (str) => str.charAt(0).toUpperCase() + str.slice(1);


    const loadReports = async () => {
        try {
            const apiSortColumn = capitalizeFirstLetter(sortColumn);
            const response = await fetchUserReports(
                loggedInUser.name,
                page,
                10,
                startDate,
                endDate,
                sortDirection,
                apiSortColumn
            );
            if (response?.success && response.data) {
                setReportData(response.data);
                setTotalPages(response.totalPages || 1);
            } else {
                setReportData([]);
                setTotalPages(1);
                console.error("Failed to load reports", response);
            }
        } catch (error) {
            console.error("Error loading reports:", error);
        }
    };

    const formatDate = (dateString) => {
        if (!dateString) return "";
        const [year, month, day] = dateString.split("-");
        return `${day}-${month}-${year}`;
    };

    const handleDownloadTemplate = async () => {
        try {
            const res = await downloadReportTemplate(loggedInUser.name);
            const blobUrl = window.URL.createObjectURL(new Blob([res.data]));
            const link = document.createElement("a");
            link.href = blobUrl;
            link.setAttribute("download", `${loggedInUser.name}-report-template.xlsx`);
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.URL.revokeObjectURL(blobUrl);
        } catch (err) {
            console.error("Template download failed", err);
            alert("Failed to download template.");
        }
    };


    const handleExportAllReports = async () => {
        try {
            let allData = [];
            let currentPage = 1;
            const pageSize = 10;
            let totalPages = 1;

            do {
                const res = await fetchUserReports(
                    loggedInUser.name,
                    currentPage,
                    pageSize,
                    startDate,
                    endDate,
                    sortDirection,
                    capitalizeFirstLetter(sortColumn)
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

            const workbook = new ExcelJS.Workbook();
            const sheetName = `${loggedInUser.name || "User"}Timesheet`;
            const worksheet = workbook.addWorksheet(sheetName);

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

            // Style headers
            headerRow.eachCell((cell) => {
                cell.font = { bold: true };
                cell.alignment = { horizontal: "center", vertical: "middle" };
                cell.fill = {
                    type: "pattern",
                    pattern: "solid",
                    fgColor: { argb: "FFCCCCCC" } // light gray
                };
                cell.border = {
                    top: { style: "medium" },
                    left: { style: "medium" },
                    bottom: { style: "medium" },
                    right: { style: "medium" }
                };
            });

            // Add data rows
            exportRows.forEach((row) => {
                const dataRow = worksheet.addRow(Object.values(row));
                dataRow.eachCell((cell) => {
                    cell.border = {
                        top: { style: "thin" },
                        left: { style: "thin" },
                        bottom: { style: "thin" },
                        right: { style: "thin" }
                    };
                    if (typeof cell.value === "number") {
                        cell.alignment = { horizontal: "right" };
                    }
                });
            });

            // <-- paste totals block here
            // Sum hours/minutes and carry minutes into hours
            const totals = exportRows.reduce(
                (acc, row) => {
                    acc.minutes += row["Man Minutes"] || 0;
                    acc.hours += row["Man Hours"] || 0;
                    return acc;
                },
                { hours: 0, minutes: 0 }
            );
            totals.hours += Math.floor(totals.minutes / 60);
            totals.minutes = totals.minutes % 60;

            const totalRow = worksheet.addRow(["Total", "", "", "", "", "", "", "", totals.hours, totals.minutes]);
            worksheet.mergeCells(totalRow.number, 1, totalRow.number, 8);
            totalRow.eachCell((cell, colNumber) => {
                cell.font = { bold: true };
                cell.alignment = { horizontal: colNumber >= 9 ? "right" : "right" };
                cell.border = {
                    top: { style: "thin" },
                    left: { style: "thin" },
                    bottom: { style: "double" },
                    right: { style: "thin" },
                };
            });

            // Auto column width
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
            console.error("Error exporting all reports:", err);
            alert("Failed to export reports. Check console for details.");
        }
    };

    const loadDropdowns = async () => {
        try {
            const [clientRes, projectRes] = await Promise.all([
                fetchClients(1),
                fetchProjects(1),
            ]);

            if (clientRes?.data) {
                setClients(clientRes.data.filter((c) => c.status?.toLowerCase() !== "inactive").map((c) => c.name));
            }

            if (projectRes?.data) {
                setProjects(projectRes.data.filter((p) => p.status?.toLowerCase() !== "inactive").map((p) => p.project1));
            }
        } catch (error) {
            console.error("Error loading dropdown data:", error);
        }
    };


    useEffect(() => {
        if (!loggedInUser?.role === "admin") return;
        loadReports();
        loadDropdowns();
        // eslint-disable-next-line
    }, [startDate, endDate, sortColumn, sortDirection, loggedInUser?.role, page]);

    const columns = [
        { header: "Date", accessor: "date", sortable: true, width: "140px" },
        { header: "Username", accessor: "username", width: "140px" },
        { header: "Client", accessor: "client", width: "120px" },
        { header: "Project / Application", accessor: "project", width: "166px" },
        { header: "Task", accessor: "task" },
        { header: "Ticket No.", accessor: "ticket", width: "110px" },
        { header: "Call", accessor: "call", width: "110px" },
        { header: "Man Hours", accessor: "manHours", width: "110px" },
        { header: "Man Minutes", accessor: "manMinutes", width: "110px" },
    ];

    const dropdownOptions = {
        client: clients,
        project: projects,
        call: ["Yes", "No"],
        manHours: ["0", "1", "2"],
        manMinutes: ["0", "15", "30", "45"],
    };

    return (
        <div className="container-fluid page p-0">
            <AdminNavbar />
            <div className="container-fluid px-4 table-wrapper">
                {/* <h5 className="text-center userhead">{loggedInUser.name} Timesheet</h5> */}
                {/* <div className="row mb-2 align-items-center">

                    <div className="col-auto d-flex align-items-center">
                        <label className="me-2" style={{ fontSize: "14px" }}>
                            Start Date
                        </label>
                        <input
                            type="date"
                            className="form-control form-control-sm"
                            style={{ width: "150px" }}
                        />
                    </div>

                    <div className="col-auto d-flex align-items-center">
                        <label className="me-2" style={{ fontSize: "14px" }}>
                            End Date
                        </label>
                        <input
                            type="date"
                            className="form-control form-control-sm"
                            style={{ width: "150px" }}
                        />
                    </div>

                </div> */}
                <FilterBar
                    showUser={false}
                    showClient={false}
                    startDate={startDate}
                    endDate={endDate}
                    onChange={({ startDate: s, endDate: e }) => {
                        setPage(1);
                        setStartDate(s);
                        setEndDate(e);
                        // loadReports(s,e);
                    }}

                    onReset={() => {
                        const today = new Date().toISOString().split("T")[0];
                        setPage(1);
                        setStartDate(today);
                        setEndDate(today);
                        setSortDirection("asc");
                        // loadReports();
                    }}
                />
                <EditableTable
                    columns={columns}
                    data={reportData}
                    setData={setReportData}
                    dropdownOptions={dropdownOptions}
                    readOnlyFields={["username"]}
                    onDataChange={loadReports}
                    defaultUsername={loggedInUser.name}
                    Isrole={loggedInUser.role === "admin" ? "admin" : ""}
                    addItem={addReport}
                    updateItem={updateReport}
                    deleteItem={deleteReport}
                    loadReports={loadReports}
                    setSortColumn={setSortColumn}
                    setSortDirection={setSortDirection}
                    sortColumn={sortColumn}
                    sortDirection={sortDirection}
                    onPageChange={(newPage) => setPage(newPage)}
                    onExportExcel={handleExportAllReports}
                    onDownloadTemplate={handleDownloadTemplate}
                    showExportButton={true}
                    showHeader={true}
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
        </div>
    );

};

export default UserPage;