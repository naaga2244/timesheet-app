using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuiperTracker.Data;
using QuiperTracker.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuiperTracker.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly QuiperTrackerContext _context;
        private readonly IConfiguration _configuration;
        //private readonly LoginAuditService _audit;

        public HomeController(ILogger<HomeController> logger, QuiperTrackerContext context, IConfiguration configuration /*, LoginAuditService audit*/)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            //_audit = audit;
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public async Task<IActionResult> Login(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { success = false, message = "Email is required" });

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (user == null)
                {
                    //await _audit.LogAsync("N/A", email, "UserNotFound");
                    return NotFound(new { success = false, message = "Email not found" });
                }

                if (user.Status.ToLower() == "inactive")
                {
                    //await _audit.LogAsync(user.Id.ToString(), user.Email, "InactiveUser");
                    return Ok(new { success = false, message = "InActive User Please contact your administrator", user = (object?)null });
                }

                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];
                var expiryMinutes = Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"]);

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role ?? "user")
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                //await _audit.LogAsync(user.Id.ToString(), user.Email, "Success");

                return Ok(new
                {
                    success = true,
                    token = jwtToken,
                    data = new
                    {
                        user.Id,
                        user.Name,
                        user.Email,
                        user.Role,
                        user.Status
                    }
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "admin,user")]
        [HttpGet("report-template")]
        public IActionResult DownloadReportTemplate(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { success = false, message = "Username is required" });

            var today = DateOnly.FromDateTime(DateTime.Today);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("ReportTemplate");
            
            var headers = new[]
            {
                "S.No", "Date", "Username", "Client", "Project", "Task",
                "Ticket No.", "Call", "Man Hours", "Man Minutes"
            };

            // -----------------------------
            // Header Row
            // -----------------------------
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            }

            // -----------------------------
            // First Data Row (Row 2)
            // -----------------------------
            ws.Cell(2, 1).FormulaA1 = "=ROW()-1";   // Auto S.No
            ws.Cell(2, 2).FormulaA1 = "=TODAY()";   // Auto Date
            ws.Cell(2, 2).Style.DateFormat.Format = "dd-MM-yyyy";
            ws.Cell(2, 3).FormulaA1 = $"=\"{username}\"";       // Auto Username

            // Borders for first data row
            for (int col = 1; col <= headers.Length; col++)
            {
                ws.Cell(2, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // -----------------------------
            // Convert to Excel Table
            // -----------------------------
            var tableRange = ws.Range(1, 1, 2, headers.Length);
            var table = tableRange.CreateTable();
            table.Name = "ReportTable";
            table.ShowAutoFilter = true;

            // -----------------------------
            // Autofit columns
            // -----------------------------
            ws.Columns().AdjustToContents();

            // -----------------------------
            // Return Excel file
            // -----------------------------
            using var stream = new MemoryStream();  
            wb.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{username}-report-template.xlsx"
            );
        }

        [Authorize(Roles = "admin,user")]
        [HttpPost("import-reports")]
        public async Task<IActionResult> ImportReports(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded." });

            var loggedInUser = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(loggedInUser))
                return Unauthorized(new { success = false, message = "Invalid user context." });

            var expectedHeaders = new[]
            {
                "S.No", "Date", "Username", "Client", "Project", "Task",
                "Ticket No.", "Call", "Man Hours", "Man Minutes"
            };

            var imported = new List<Report>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();

                // ----------------------------------------
                // 1. Validate Table Exists
                // ----------------------------------------
                var table = ws.Tables.FirstOrDefault(t => t.Name == "ReportTable");
                if (table == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid template. Excel table 'ReportTable' not found."
                    });
                }

                // ----------------------------------------
                // 2. Validate Headers (No extra / missing columns)
                // ----------------------------------------
                if (table.Fields.Count() != expectedHeaders.Length)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid template. Column count mismatch."
                    });
                }

                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    var header = table.HeadersRow().Cell(i + 1).GetString().Trim();
                    if (!string.Equals(header, expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Invalid template. Column {i + 1} must be '{expectedHeaders[i]}'."
                        });
                    }
                }

                // ----------------------------------------
                // 3. Process Table Rows
                // ----------------------------------------
                foreach (var row in table.DataRange.Rows())
                {
                    // Skip rows where user-input columns are empty
                    if (row.Cells(4, 10).All(c => c.IsEmpty()))
                        continue;

                    // ---------- Date parsing ----------
                    DateOnly date;
                    var dateCell = row.Cell(2);

                    if (dateCell.TryGetValue(out DateTime excelDate))
                    {
                        date = DateOnly.FromDateTime(excelDate);
                    }
                    else
                    {
                        var dateStr = dateCell.GetString();
                        if (!DateOnly.TryParseExact(
                                dateStr,
                                new[] { "dd-MM-yyyy", "yyyy-MM-dd" },
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out date))
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = $"Invalid date format at row {row.RowNumber()}."
                            });
                        }
                    }

                    // ---------- Numeric parsing ----------
                    //int.TryParse(row.Cell(9).GetString(), out var manHours);
                    //int.TryParse(row.Cell(10).GetString(), out var manMinutes);


                    imported.Add(new Report
                    {
                        Date = date,
                        Username = loggedInUser,          // 🔒 DO NOT trust Excel
                        Client = row.Cell(4).GetString(),
                        Project = row.Cell(5).GetString(),
                        Task = row.Cell(6).GetString(),
                        Ticket = row.Cell(7).GetString(),
                        Call = row.Cell(8).GetString(),
                        ManHours = row.Cell(9).GetString(),
                        ManMinutes = row.Cell(10).GetString()
                    });
                }

                if (!imported.Any())
                    return BadRequest(new { success = false, message = "No valid data rows found." });

                // ----------------------------------------
                // 4. Save to DB
                // ----------------------------------------
                _context.Reports.AddRange(imported);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Reports imported successfully.",
                    count = imported.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to import Excel file.",
                    error = ex.Message
                });
            }
        }

        //[Authorize(Roles = "admin")]
        //[HttpGet("fetch-reports")]
        //public async Task<ActionResult<IEnumerable<Report>>> GetReports(
        //    int pageNumber = 1,
        //    int pageSize = 10,
        //    string sortColumn = "Date",
        //    string sortDirection = "asc",
        //    string username = "",
        //    string client = "",
        //    string startDate = "",
        //    string endDate = "")
        //{
        //    try
        //    {
        //        if (pageNumber < 1) pageNumber = 1;
        //        sortDirection = sortDirection?.ToLower() == "desc" ? "desc" : "asc";
        //        bool isDescending = sortDirection == "desc";

        //        var validSortColumns = new List<string>
        //        {
        //            "Date", "Username", "Client", "Project", "Task", "Ticket",
        //            "Call", "ManHours", "ManMinutes"
        //        };

        //        if (!validSortColumns.Contains(sortColumn))
        //            sortColumn = "Date";

        //        IQueryable<Report> query = _context.Reports.AsQueryable();

        //        if (!string.IsNullOrEmpty(username))
        //            query = query.Where(x => x.Username == username);

        //        if (!string.IsNullOrEmpty(client))
        //            query = query.Where(x => x.Client == client);

        //        if (DateOnly.TryParse(startDate, out var sDate))
        //            query = query.Where(x => x.Date >= sDate);

        //        if (DateOnly.TryParse(endDate, out var eDate))
        //            query = query.Where(x => x.Date <= eDate);

        //        query = sortColumn switch
        //        {
        //            "Date" => isDescending ? query.OrderByDescending(r => r.Date) : query.OrderBy(r => r.Date),
        //            "Username" => isDescending ? query.OrderByDescending(r => r.Username) : query.OrderBy(r => r.Username),
        //            "Client" => isDescending ? query.OrderByDescending(r => r.Client) : query.OrderBy(r => r.Client),
        //            "Project" => isDescending ? query.OrderByDescending(r => r.Project) : query.OrderBy(r => r.Project),
        //            "Task" => isDescending ? query.OrderByDescending(r => r.Task) : query.OrderBy(r => r.Task),
        //            "Ticket" => isDescending ? query.OrderByDescending(r => r.Ticket) : query.OrderBy(r => r.Ticket),
        //            "Call" => isDescending ? query.OrderByDescending(r => r.Call) : query.OrderBy(r => r.Call),
        //            "ManHours" => isDescending ? query.OrderByDescending(r => r.ManHours) : query.OrderBy(r => r.ManHours),
        //            "ManMinutes" => isDescending ? query.OrderByDescending(r => r.ManMinutes) : query.OrderBy(r => r.ManMinutes),
        //            _ => query.OrderBy(r => r.Date)
        //        };

        //        // ✅ Pagination
        //        int totalCount = await query.CountAsync();
        //        var result = await query
        //            .Skip((pageNumber - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToListAsync();

        //        //var formattedResult = result.Select(r => new
        //        //{
        //        //    r.Id,
        //        //    Date = r.Date.ToString("dd-MM-yyyy"),
        //        //    r.Username,
        //        //    r.Client,
        //        //    r.Project,
        //        //    r.Task,
        //        //    r.Ticket,
        //        //    r.Call,
        //        //    r.ManHours,
        //        //    r.ManMinutes
        //        //});

        //        return Ok(new
        //        {
        //            success = true,
        //            data = result,
        //            totalCount = totalCount,
        //            pageNumber = pageNumber,
        //            pageSize = pageSize,
        //            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //}

        [Authorize(Roles = "admin,user")]
        [HttpPost("add-report")]
        public async Task<ActionResult> AddReport([FromBody] Report newreport)
        {
            try
            {
                if (newreport == null)
                    return BadRequest(new
                    { success = false, message = "Invalid Report Data" }
                    );
                if (newreport.Date == default)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid report data."
                    }
                    );

                _context.Reports.Add(newreport);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Report added successfully",
                    data = newreport
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    new { success = false, message = ex.Message }
                );
            }
        }

        //[Authorize(Roles = "user")]
        //[HttpGet("userreports")]
        //public async Task<ActionResult<IEnumerable<Report>>> GetUsersReport(
        //    string username,
        //    int pageNumber = 1,
        //    int pageSize = 10,
        //    string sortColumn = "Date",
        //    string sortDirection = "asc",
        //    string startDate = "",
        //    string endDate = "")
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(username))
        //            return BadRequest(new
        //            {
        //                success = false,
        //                message = "Username is required"
        //            });
        //        if (pageNumber < 1) pageNumber = 1;

        //        IQueryable<Report> query = _context.Reports
        //            .Where(r => r.Username.ToLower() == username.ToLower());

        //        if (DateOnly.TryParse(startDate, out var sDate))
        //            query = query.Where(x => x.Date >= sDate);

        //        if (DateOnly.TryParse(endDate, out var eDate))
        //            query = query.Where(x => x.Date <= eDate);

        //        sortDirection = sortDirection.ToLower() == "asc" ? "asc" : "desc";
        //        bool isDescending = sortDirection == "desc";

        //        var validSortColumns = new List<string>
        //        {
        //            "Date", "Username", "Client", "Project", "Task",
        //            "Ticket", "Call", "ManHours", "ManMinutes"
        //        };

        //        if (!validSortColumns.Contains(sortColumn))
        //            sortColumn = "Id";

        //        query = sortColumn switch
        //        {
        //            "Date" => isDescending ? query.OrderByDescending(r => r.Date) : query.OrderBy(r => r.Date),
        //            "Username" => isDescending ? query.OrderByDescending(r => r.Username) : query.OrderBy(r => r.Username),
        //            "Client" => isDescending ? query.OrderByDescending(r => r.Client) : query.OrderBy(r => r.Client),
        //            "Project" => isDescending ? query.OrderByDescending(r => r.Project) : query.OrderBy(r => r.Project),
        //            "Task" => isDescending ? query.OrderByDescending(r => r.Task) : query.OrderBy(r => r.Task),
        //            "Ticket" => isDescending ? query.OrderByDescending(r => r.Ticket) : query.OrderBy(r => r.Ticket),
        //            "Call" => isDescending ? query.OrderByDescending(r => r.Call) : query.OrderBy(r => r.Call),
        //            "ManHours" => isDescending ? query.OrderByDescending(r => r.ManHours) : query.OrderBy(r => r.ManHours),
        //            "ManMinutes" => isDescending ? query.OrderByDescending(r => r.ManMinutes) : query.OrderBy(r => r.ManMinutes),
        //            _ => query.OrderBy(r => r.Date)
        //        };

        //        int totalCount = await query.CountAsync();
        //        var result = await query
        //            .Skip((pageNumber - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToListAsync();

        //        //var formattedResult = result.Select(r => new
        //        //{
        //        //    r.Id,
        //        //    Date = r.Date.ToString("dd-MM-yyyy"),
        //        //    r.Username,
        //        //    r.Client,
        //        //    r.Project,
        //        //    r.Task,
        //        //    r.Ticket,
        //        //    r.Call,
        //        //    r.ManHours,
        //        //    r.ManMinutes
        //        //});

        //        return Ok(new
        //        {
        //            success = true,
        //            data = result,
        //            totalCount = totalCount,
        //            pageNumber = pageNumber,
        //            pageSize = pageSize,
        //            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        //        });

        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

        [Authorize(Roles = "admin,user")]
        [HttpDelete("delete-report/{id}")]
        public async Task<ActionResult> DeleteReport(int id)
        {
            try
            {
                var report = await _context.Reports.FindAsync(id);
                if (report == null)
                {
                    return NotFound(new { success = false, message = "Report not found" });
                }

                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();

                return Ok(
                    new
                    {
                        success = true,
                        message = "Report deleted successfully"
                    }
                );

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "admin,user")]
        [HttpPut("update-report/{id}")]
        public async Task<ActionResult> UpdateReport(int id,
            [FromBody] Report updatedReport)
        {
            try
            {
                if (updatedReport == null)
                    return BadRequest("Invalid report data.");

                var existing = _context.Reports.FirstOrDefault(r => r.Id == id);
                if (existing == null)
                    return NotFound($"Report with ID {id} not found.");

                var props = typeof(Report).GetProperties();
                foreach (var prop in props)
                {
                    var newValue = prop.GetValue(updatedReport);
                    if (newValue == null)
                        continue;

                    // Skip empty strings
                    if (prop.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)newValue))
                        continue;

                    // Update only changed fields
                    prop.SetValue(existing, newValue);
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Report updated successfully", data = existing });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "admin,user")]
        [HttpGet("fetch-roles")]
        public ActionResult<IEnumerable<Role>> GetRoles(
            int pageNumber = 1,
            int pageSize = 10,
            string sortColumn = "Id",
            string sortDirection = "asc")
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                sortDirection = sortDirection.ToLower() == "desc" ? "desc" : "asc";
                bool isDescending = sortDirection == "desc";

                var validSortColumns = new List<string>
                {
                    "Id", "Role"
                };

                if (!validSortColumns.Contains(sortColumn))
                    sortColumn = "Id";

                IQueryable<Role> query = _context.Roles.AsQueryable();

                query = sortColumn switch
                {
                    "Role" => isDescending ? query.OrderByDescending(r => r.Role1) : query.OrderBy(r => r.Role1),
                    _ => isDescending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id)
                };

                int totalCount = query.Count();

                var result = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

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

        //[Authorize(Roles = "admin")]
        //[HttpPut("update-role/{id}")]
        //public async Task<ActionResult> UpdateRole(int id,
        //    [FromBody] Role updatedRole)
        //{
        //    try
        //    {
        //        if (updatedRole == null)
        //            return BadRequest("Invalid Role");

        //        var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        //        if (existing == null)
        //            return NotFound($"Role with ID {id} not found");
        //        var props = typeof(Role).GetProperties();

        //        foreach (var prop in props)
        //        {
        //            var newValue = prop.GetValue(updatedRole);
        //            if (newValue == null)
        //                continue;

        //            // Skip empty strings
        //            if (prop.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)newValue))
        //                continue;

        //            // Update only changed fields
        //            prop.SetValue(existing, newValue);
        //        }

        //        await _context.SaveChangesAsync();
        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Role updated successfully",
        //            data = existing
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

        //[Authorize(Roles = "admin")]
        //[HttpPost("add-role")]
        //public async Task<ActionResult> AddRole([FromBody] Role newRole)
        //{
        //    try
        //    {
        //        if (newRole == null || String.IsNullOrWhiteSpace(newRole.Role1))
        //            return BadRequest(new { success = false, message = "Invalid role data." });

        //        if (string.IsNullOrWhiteSpace(newRole.Status))
        //            newRole.Status = "Inactive";

        //        _context.Roles.Add(newRole);
        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Role added successfully",
        //            data = newRole
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}

        [Authorize(Roles = "admin,user")]
        [HttpGet("fetch-users")]
        public ActionResult<IEnumerable<User>> GetUsers(
            int pageNumber = 1,
            int pageSize = 10,
            string sortColumn = "Id",
            string sortDirection = "asc")
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                sortDirection = sortDirection.ToLower() == "desc" ? "desc" : "asc";
                bool isDescending = sortDirection == "desc";

                var validSortColumns = new List<string>
                {
                    "Id", "Name", "Email"
                };
                if (!validSortColumns.Contains(sortColumn))
                    sortColumn = "Id";

                IQueryable<User> query = _context.Users.AsQueryable();
                query = sortColumn switch
                {
                    "Name" => isDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
                    "Email" => isDescending ? query.OrderByDescending(r => r.Email) : query.OrderBy(r => r.Email),
                    _ => isDescending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id)

                };

                int totalCount = query.Count();

                var result = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

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

        //[Authorize(Roles = "admin")]
        //[HttpPost("add-user")]
        //public async Task<ActionResult> AddUsers([FromBody] User newUser)
        //{
        //    try
        //    {
        //        if (newUser == null || string.IsNullOrWhiteSpace(newUser.Name)
        //            || string.IsNullOrWhiteSpace(newUser.Email))
        //            return BadRequest(new { success = false, message = "Invalid user data." });

        //        if (string.IsNullOrWhiteSpace(newUser.Status))
        //            newUser.Status = "InActive";

        //        // Check if email already exists
        //        bool emailExists = await _context.Users.AnyAsync(u => u.Email == newUser.Email);
        //        if (emailExists)
        //            return BadRequest(new { success = false, message = "Email already exists." });

        //        _context.Users.Add(newUser);
        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "User added successfully",
        //            data = newUser
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}

        //[Authorize(Roles = "admin")]
        //[HttpPut("update-user/{id}")]
        //public async Task<ActionResult> UpdateUser(int id,
        //    [FromBody] User updatedUser)
        //{
        //    try
        //    {
        //        if (updatedUser == null)
        //            return BadRequest(new { success = false, message = "Invalid user data." });

        //        var existing = await _context.Users.FindAsync(id);
        //        if (existing == null)
        //            return NotFound(new { success = false, message = $"User with ID {id} not found." });

        //        // Check for duplicate email excluding current user
        //        if (!string.IsNullOrWhiteSpace(updatedUser.Email))
        //        {
        //            bool emailExists = await _context.Users
        //                .AnyAsync(u => u.Email == updatedUser.Email && u.Id != id);
        //            if (emailExists)
        //                return BadRequest(new { success = false, message = "Email already exists." });
        //        }

        //        var props = typeof(User).GetProperties();

        //        foreach (var prop in props)
        //        {
        //            var newValue = prop.GetValue(updatedUser);
        //            if (newValue == null)
        //                continue;

        //            // Skip empty strings
        //            if (prop.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)newValue))
        //                continue;

        //            // Update only changed fields
        //            prop.SetValue(existing, newValue);
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "User updated successfully",
        //            data = existing
        //        });

        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}

        [Authorize(Roles = "admin,user")]
        [HttpGet("fetch-clients")]
        public ActionResult<IEnumerable<Client>> GetClients(
            int pageNumber = 1,
            int pageSize = 10,
            string sortColumn = "Id",
            string sortDirection = "asc")
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                sortDirection = sortDirection.ToLower() == "desc" ? "desc" : "asc";
                bool isDescending = sortDirection == "desc";

                var validSortColumns = new List<string>
                {
                    "Id",  "Name"
                };

                if (!validSortColumns.Contains(sortColumn))
                    sortColumn = "Id";

                IQueryable<Client> query = _context.Clients.AsQueryable();

                query = sortColumn switch
                {
                    "Name" => isDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
                    _ => isDescending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id)

                };

                int totalCount = query.Count();

                var result = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

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

        //[Authorize(Roles = "admin")]
        //[HttpPost("add-client")]
        //public async Task<ActionResult> AddClient([FromBody] Client newClient)
        //{
        //    try
        //    {
        //        if (newClient == null || string.IsNullOrWhiteSpace(newClient.Name))
        //            return BadRequest(new { success = false, message = "Invalid client data." });

        //        if (string.IsNullOrWhiteSpace(newClient.Status))
        //            newClient.Status = "InActive";

        //        _context.Clients.Add(newClient);
        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Client added successfully",
        //            data = newClient
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //}

        //[Authorize(Roles = "admin")]
        //[HttpPut("update-client/{id}")]
        //public async Task<ActionResult> UpdateClient(int id,
        //    [FromBody] Client updatedClient)
        //{
        //    try
        //    {
        //        if (updatedClient == null)
        //            return BadRequest(new { success = false, message = "Invalid client data." });

        //        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        //        if (existingClient == null)
        //            return NotFound(new { success = false, message = $"Client with ID {id} not found." });

        //        // Update properties only if not null/empty
        //        if (!string.IsNullOrWhiteSpace(updatedClient.Name))
        //            existingClient.Name = updatedClient.Name;

        //        if (!string.IsNullOrWhiteSpace(updatedClient.Status))
        //            existingClient.Status = updatedClient.Status;

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Client updated successfully",
        //            data = existingClient
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}

        [Authorize(Roles = "admin,user")]
        [HttpGet("fetch-projects")]
        public ActionResult<IEnumerable<Project>> GetProjects(
            int pageNumber = 1,
            int pageSize = 10,
            string sortColumn = "Id",
            string sortDirection = "asc")
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                sortDirection = sortDirection.ToLower() == "desc" ? "desc" : "asc";
                bool isDescending = sortDirection == "desc";

                var validSortColumns = new List<string>
                {
                    "Id",  "Project", "Client"
                };

                if (!validSortColumns.Contains(sortColumn))
                    sortColumn = "Id";

                IQueryable<Project> query = _context.Projects.AsQueryable();
                query = sortColumn switch
                {
                    "Project" => isDescending ? query.OrderByDescending(r => r.Project1) : query.OrderBy(r => r.Project1),
                    "Client" => isDescending ? query.OrderByDescending(r => r.Client) : query.OrderBy(r => r.Client),
                    _ => isDescending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id)
                };

                int totalCount = query.Count();

                var result = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

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

        //[Authorize(Roles = "admin")]
        //[HttpPost("add-project")]
        //public async Task<ActionResult> AddProject([FromBody] Project newProject)
        //{
        //    try
        //    {
        //        if (newProject == null || string.IsNullOrWhiteSpace(newProject.Project1) || string.IsNullOrWhiteSpace(newProject.Client))
        //            return BadRequest(new { success = false, message = "Invalid project data." });

        //        // Default status to "Active" if not provided
        //        if (string.IsNullOrWhiteSpace(newProject.Status))
        //            newProject.Status = "Active";

        //        _context.Projects.Add(newProject);
        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Project added successfully",
        //            data = newProject
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}

        //[Authorize(Roles = "admin")]
        //[HttpPut("update-project/{id}")]
        //public async Task<ActionResult> UpdateProject(int id,
        //    [FromBody] Project updatedProject)
        //{
        //    try
        //    {
        //        if (updatedProject == null)
        //            return BadRequest(new { success = false, message = "Invalid project data." });

        //        var existing = await _context.Projects.FindAsync(id);
        //        if (existing == null)
        //            return NotFound(new { success = false, message = $"Project with ID {id} not found." });

        //        // Update only changed fields
        //        existing.Project1 = string.IsNullOrWhiteSpace(updatedProject.Project1) ? existing.Project1 : updatedProject.Project1;
        //        existing.Client = string.IsNullOrWhiteSpace(updatedProject.Client) ? existing.Client : updatedProject.Client;
        //        existing.Status = string.IsNullOrWhiteSpace(updatedProject.Status) ? existing.Status : updatedProject.Status;

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Project updated successfully",
        //            data = existing
        //        });

        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}
    }
}
