using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuiperTracker.Data;
using QuiperTracker.Models;

namespace QuiperTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "user")]
    public class UserController : ControllerBase
    {
        private readonly QuiperTrackerContext _context;

        public UserController(QuiperTrackerContext context)
        {
            _context = context;
        }

        [HttpGet("userreports")]
        public async Task<ActionResult<IEnumerable<Report>>> GetUsersReport(
            string username,
            int pageNumber = 1,
            int pageSize = 10,
            string sortColumn = "Date",
            string sortDirection = "asc",
            string startDate = "",
            string endDate = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest(new
                    {
                        success = false,
                        message = "Username is required"
                    });
                if (pageNumber < 1) pageNumber = 1;

                IQueryable<Report> query = _context.Reports
                    .Where(r => r.Username.ToLower() == username.ToLower());

                if (DateOnly.TryParse(startDate, out var sDate))
                    query = query.Where(x => x.Date >= sDate);

                if (DateOnly.TryParse(endDate, out var eDate))
                    query = query.Where(x => x.Date <= eDate);

                sortDirection = sortDirection.ToLower() == "asc" ? "asc" : "desc";
                bool isDescending = sortDirection == "desc";

                var validSortColumns = new List<string>
                {
                    "Date", "Username", "Client", "Project", "Task",
                    "Ticket", "Call", "ManHours", "ManMinutes"
                };

                if (!validSortColumns.Contains(sortColumn))
                    sortColumn = "Id";

                query = sortColumn switch
                {
                    "Date" => isDescending ? query.OrderByDescending(r => r.Date) : query.OrderBy(r => r.Date),
                    "Username" => isDescending ? query.OrderByDescending(r => r.Username) : query.OrderBy(r => r.Username),
                    "Client" => isDescending ? query.OrderByDescending(r => r.Client) : query.OrderBy(r => r.Client),
                    "Project" => isDescending ? query.OrderByDescending(r => r.Project) : query.OrderBy(r => r.Project),
                    "Task" => isDescending ? query.OrderByDescending(r => r.Task) : query.OrderBy(r => r.Task),
                    "Ticket" => isDescending ? query.OrderByDescending(r => r.Ticket) : query.OrderBy(r => r.Ticket),
                    "Call" => isDescending ? query.OrderByDescending(r => r.Call) : query.OrderBy(r => r.Call),
                    "ManHours" => isDescending ? query.OrderByDescending(r => r.ManHours) : query.OrderBy(r => r.ManHours),
                    "ManMinutes" => isDescending ? query.OrderByDescending(r => r.ManMinutes) : query.OrderBy(r => r.ManMinutes),
                    _ => query.OrderBy(r => r.Date)
                };

                int totalCount = await query.CountAsync();
                var result = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                //var formattedResult = result.Select(r => new
                //{
                //    r.Id,
                //    Date = r.Date.ToString("dd-MM-yyyy"),
                //    r.Username,
                //    r.Client,
                //    r.Project,
                //    r.Task,
                //    r.Ticket,
                //    r.Call,
                //    r.ManHours,
                //    r.ManMinutes
                //});

                return Ok(new
                {
                    success = true,
                    data = result,
                    totalCount = totalCount,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });

            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


    }
}
