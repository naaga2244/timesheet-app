using System;
using System.Collections.Generic;

namespace QuiperTracker.Models;

public partial class Project
{
    public int Id { get; set; }

    public string Project1 { get; set; } = null!;

    public string Client { get; set; } = null!;

    public string Status { get; set; } = null!;
}
