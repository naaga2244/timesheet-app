using System;
using System.Collections.Generic;

namespace QuiperTracker.Models;

public partial class Report
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public string Username { get; set; } = null!;

    public string? Client { get; set; }

    public string? Project { get; set; }

    public string? Task { get; set; }

    public string? Ticket { get; set; }

    public string? Call { get; set; }

    public string? ManHours { get; set; }

    public string? ManMinutes { get; set; }
}
