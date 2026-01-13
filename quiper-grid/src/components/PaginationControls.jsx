import React from "react";
import { FaChevronLeft, FaChevronRight } from "react-icons/fa";

const PaginationControls = ({ page, totalPages, onPageChange }) => {

    const maxPagesToShow = 5;
    let startPage = Math.max(1, page - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(totalPages, startPage + maxPagesToShow - 1);

    if (page <= 5) {
        // Show 1–5 always when page is <= 5
        startPage = 1;
        endPage = Math.min(totalPages, maxPagesToShow);
    } else {
        // Start sliding after page 5
        startPage = page - 4;
        endPage = page;

        // Ensure last part doesn't exceed limit
        if (endPage > totalPages) {
            endPage = totalPages;
            startPage = Math.max(1, endPage - maxPagesToShow + 1);
        }
    }

    console.log(`${startPage} to  ${endPage}`)

    return (
        <div className="pagination-footer d-flex justify-content-center align-items-center gap-3">
            <span>
                Page {page} of {totalPages}
            </span>
            <button
                className="btn btn-outline-dark btn-sm"
                disabled={page === 1}
                onClick={() => onPageChange(1)}
            >
                First
            </button>
            <button
                className="btn btn-outline-dark btn-sm me-2"
                disabled={page <= 1}
                onClick={() => onPageChange(page - 1)}
            >
                <FaChevronLeft/>
            </button>
            {Array.from({ length: endPage - startPage + 1 }, (_, i) => startPage + i).map((pageNumber) => (
                <button
                    key={pageNumber}
                    className={`btn btn-outline-dark btn-sm ${pageNumber === page ? 'active' : ''}`}
                    onClick={() => onPageChange(pageNumber)}
                >
                    {pageNumber}
                </button>
            ))}
            <button
                className="btn btn-outline-dark btn-sm ms-2"
                disabled={page >= totalPages}
                onClick={() => onPageChange(page + 1)}
            >
                <FaChevronRight/>
            </button>
             <button
                className="btn btn-outline-dark btn-sm"
                disabled={page === totalPages}
                onClick={() => onPageChange(totalPages)}
            >
                Last
            </button>
        </div>
    );
}
export default PaginationControls;