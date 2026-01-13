using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QuiperTracker.Models;

namespace QuiperTracker.Data;

public partial class QuiperTrackerContext : DbContext
{
    public QuiperTrackerContext()
    {
    }

    public QuiperTrackerContext(DbContextOptions<QuiperTrackerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Data Source=localhost,1433;Database=QuiperTracker;User Id=sa;Password=yourStrong(!)Password;Encrypt=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Clients__3214EC07117A1261");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Projects__3214EC076C949566");

            entity.Property(e => e.Client)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Project1)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("Project");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reports__3214EC071108AD5D");

            entity.Property(e => e.Call)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Client)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Date).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ManHours)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.ManMinutes)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.Project)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Task)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Ticket)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07DD1B03C0");

            entity.Property(e => e.Role1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC078AD40ADC");

            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("123456");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
