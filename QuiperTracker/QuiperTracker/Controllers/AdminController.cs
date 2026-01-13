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
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly QuiperTrackerContext _context;
        public AdminController(QuiperTrackerContext context)
        {
            _context = context;
        }

        [HttpGet("fetch-reports")]
        public async Task<ActionResult<IEnumerable<Report>>> GetReports(
            int pageNumber = 1,
            int pageSize = 10,
            string sortColumn = "Date",
            string sortDirection = "asc",
            string username = "",
            string client = "",
            string startDate = "",
            string endDate = "")
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                sortDirection = sortDirection?.ToLower() == "desc" ? "desc" : "asc";
                bool isDescending = sortDirection == "desc";

                var validSortColumns = new List<string>
                {
                    "Date", "Username", "Client", "Project", "Task", "Ticket",
                    "Call", "ManHours", "ManMinutes"
                };

                if (!validSortColumns.Contains(sortColumn))
                    sortColumn = "Date";

                IQueryable<Report> query = _context.Reports.AsQueryable();

                if (!string.IsNullOrEmpty(username))
                    query = query.Where(x => x.Username == username);

                if (!string.IsNullOrEmpty(client))
                    query = query.Where(x => x.Client == client);

                if (DateOnly.TryParse(startDate, out var sDate))
                    query = query.Where(x => x.Date >= sDate);

                if (DateOnly.TryParse(endDate, out var eDate))
                    query = query.Where(x => x.Date <= eDate);

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

                // ✅ Pagination
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
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("update-role/{id}")]
        public async Task<ActionResult> UpdateRole(int id,
            [FromBody] Role updatedRole)
        {
            try
            {
                if (updatedRole == null)
                    return BadRequest("Invalid Role");

                var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
                if (existing == null)
                    return NotFound($"Role with ID {id} not found");
                var props = typeof(Role).GetProperties();

                foreach (var prop in props)
                {
                    var newValue = prop.GetValue(updatedRole);
                    if (newValue == null)
                        continue;

                    // Skip empty strings
                    if (prop.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)newValue))
                        continue;

                    // Update only changed fields
                    prop.SetValue(existing, newValue);
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    message = "Role updated successfully",
                    data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("add-role")]
        public async Task<ActionResult> AddRole([FromBody] Role newRole)
        {
            try
            {
                if (newRole == null || String.IsNullOrWhiteSpace(newRole.Role1))
                    return BadRequest(new { success = false, message = "Invalid role data." });

                if (string.IsNullOrWhiteSpace(newRole.Status))
                    newRole.Status = "Inactive";

                _context.Roles.Add(newRole);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Role added successfully",
                    data = newRole
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("add-user")]
        public async Task<ActionResult> AddUsers([FromBody] User newUser)
        {
            try
            {
                if (newUser == null || string.IsNullOrWhiteSpace(newUser.Name)
                    || string.IsNullOrWhiteSpace(newUser.Email))
                    return BadRequest(new { success = false, message = "Invalid user data." });

                if (string.IsNullOrWhiteSpace(newUser.Status))
                    newUser.Status = "InActive";

                // Check if email already exists
                bool emailExists = await _context.Users.AnyAsync(u => u.Email == newUser.Email);
                if (emailExists)
                    return BadRequest(new { success = false, message = "Email already exists." });

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "User added successfully",
                    data = newUser
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("update-user/{id}")]
        public async Task<ActionResult> UpdateUser(int id,
            [FromBody] User updatedUser)
        {
            try
            {
                if (updatedUser == null)
                    return BadRequest(new { success = false, message = "Invalid user data." });

                var existing = await _context.Users.FindAsync(id);
                if (existing == null)
                    return NotFound(new { success = false, message = $"User with ID {id} not found." });

                // Check for duplicate email excluding current user
                if (!string.IsNullOrWhiteSpace(updatedUser.Email))
                {
                    bool emailExists = await _context.Users
                        .AnyAsync(u => u.Email == updatedUser.Email && u.Id != id);
                    if (emailExists)
                        return BadRequest(new { success = false, message = "Email already exists." });
                }

                var props = typeof(User).GetProperties();

                foreach (var prop in props)
                {
                    var newValue = prop.GetValue(updatedUser);
                    if (newValue == null)
                        continue;

                    // Skip empty strings
                    if (prop.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)newValue))
                        continue;

                    // Update only changed fields
                    prop.SetValue(existing, newValue);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "User updated successfully",
                    data = existing
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("add-client")]
        public async Task<ActionResult> AddClient([FromBody] Client newClient)
        {
            try
            {
                if (newClient == null || string.IsNullOrWhiteSpace(newClient.Name))
                    return BadRequest(new { success = false, message = "Invalid client data." });

                if (string.IsNullOrWhiteSpace(newClient.Status))
                    newClient.Status = "InActive";

                _context.Clients.Add(newClient);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Client added successfully",
                    data = newClient
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("update-client/{id}")]
        public async Task<ActionResult> UpdateClient(int id,
            [FromBody] Client updatedClient)
        {
            try
            {
                if (updatedClient == null)
                    return BadRequest(new { success = false, message = "Invalid client data." });

                var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
                if (existingClient == null)
                    return NotFound(new { success = false, message = $"Client with ID {id} not found." });

                // Update properties only if not null/empty
                if (!string.IsNullOrWhiteSpace(updatedClient.Name))
                    existingClient.Name = updatedClient.Name;

                if (!string.IsNullOrWhiteSpace(updatedClient.Status))
                    existingClient.Status = updatedClient.Status;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Client updated successfully",
                    data = existingClient
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("add-project")]
        public async Task<ActionResult> AddProject([FromBody] Project newProject)
        {
            try
            {
                if (newProject == null || string.IsNullOrWhiteSpace(newProject.Project1) || string.IsNullOrWhiteSpace(newProject.Client))
                    return BadRequest(new { success = false, message = "Invalid project data." });

                // Default status to "Active" if not provided
                if (string.IsNullOrWhiteSpace(newProject.Status))
                    newProject.Status = "Active";

                _context.Projects.Add(newProject);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Project added successfully",
                    data = newProject
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("update-project/{id}")]
        public async Task<ActionResult> UpdateProject(int id,
            [FromBody] Project updatedProject)
        {
            try
            {
                if (updatedProject == null)
                    return BadRequest(new { success = false, message = "Invalid project data." });

                var existing = await _context.Projects.FindAsync(id);
                if (existing == null)
                    return NotFound(new { success = false, message = $"Project with ID {id} not found." });

                // Update only changed fields
                existing.Project1 = string.IsNullOrWhiteSpace(updatedProject.Project1) ? existing.Project1 : updatedProject.Project1;
                existing.Client = string.IsNullOrWhiteSpace(updatedProject.Client) ? existing.Client : updatedProject.Client;
                existing.Status = string.IsNullOrWhiteSpace(updatedProject.Status) ? existing.Status : updatedProject.Status;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Project updated successfully",
                    data = existing
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

    }
}
