using System;
using System.Collections.Generic;

namespace QuiperTracker.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Password { get; set; }

    public int LoginStatus { get; set; }
}
