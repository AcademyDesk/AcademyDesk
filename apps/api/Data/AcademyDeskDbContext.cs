using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Data;

public sealed class AcademyDeskDbContext(DbContextOptions<AcademyDeskDbContext> options)
    : DbContext(options)
{
    public DbSet<Academy> Academies => Set<Academy>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ProgramCourse> Courses => Set<ProgramCourse>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<FeePlan> FeePlans => Set<FeePlan>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MusicPiece> MusicPieces => Set<MusicPiece>();
    public DbSet<StudentMusicProgress> StudentMusicProgress => Set<StudentMusicProgress>();
    public DbSet<AcademyEvent> AcademyEvents => Set<AcademyEvent>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<MakeupClass> MakeupClasses => Set<MakeupClass>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Academy>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(250);
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.TimeZone).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120);
            entity.Property(x => x.State).HasMaxLength(120);
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
            entity.HasOne(x => x.Academy)
                .WithMany(x => x.Branches)
                .HasForeignKey(x => x.AcademyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.ProgramInterest).HasMaxLength(200);
            entity.Property(x => x.Source).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Stage).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.HasIndex(x => new { x.AcademyId, x.Stage, x.FollowUpAtUtc });
            entity.HasIndex(x => new { x.AcademyId, x.Email });
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.HasIndex(x => new { x.AcademyId, x.LastName, x.FirstName });
            entity.HasIndex(x => new { x.AcademyId, x.Email });
        });

        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.HasIndex(x => new { x.AcademyId, x.LastName, x.FirstName });
        });

        modelBuilder.Entity<StudentGuardian>(entity =>
        {
            entity.Property(x => x.Relationship).HasMaxLength(50);
            entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.GuardianId }).IsUnique();
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Guardian).WithMany().HasForeignKey(x => x.GuardianId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Specialties).HasMaxLength(500);
            entity.HasIndex(x => new { x.AcademyId, x.LastName, x.FirstName });
        });

        modelBuilder.Entity<ProgramCourse>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AcademyType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Level).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.BatchId, x.Status });
        });

        modelBuilder.Entity<ClassSession>(entity =>
        {
            entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.RoomName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.StartUtc });
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.AcademyId, x.ClassSessionId, x.StudentId }).IsUnique();
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.BatchId, x.DueAtUtc });
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.BatchId, x.ScheduledAtUtc });
        });

        modelBuilder.Entity<AssessmentResult>(entity =>
        {
            entity.Property(x => x.Grade).HasMaxLength(30);
            entity.Property(x => x.Remarks).HasMaxLength(1000);
            entity.HasIndex(x => new { x.AcademyId, x.AssessmentId, x.StudentId }).IsUnique();
        });

        modelBuilder.Entity<FeePlan>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Frequency).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(x => x.InvoiceNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.InvoiceNumber }).IsUnique();
            entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.Status });
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Method).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(150);
            entity.HasIndex(x => new { x.AcademyId, x.InvoiceId, x.PaidAtUtc });
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.ExpenseDate });
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.RecipientType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Channel).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.RecipientId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MetadataJson).HasMaxLength(8000);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.HasIndex(x => new { x.AcademyId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.AcademyId, x.EntityType, x.EntityId });
        });

        modelBuilder.Entity<MusicPiece>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Composer).HasMaxLength(200);
            entity.Property(x => x.Instrument).HasMaxLength(100);
            entity.Property(x => x.Genre).HasMaxLength(100);
            entity.Property(x => x.Difficulty).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.Title });
        });

        modelBuilder.Entity<StudentMusicProgress>(entity =>
        {
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.Score).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.MusicPieceId }).IsUnique();
        });

        modelBuilder.Entity<AcademyEvent>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Venue).HasMaxLength(250);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.HasIndex(x => new { x.AcademyId, x.StartUtc });
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.Property(x => x.CertificateNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => new { x.AcademyId, x.CertificateNumber }).IsUnique();
            entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.IssuedDate });
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.Property(x => x.RequesterType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.DecisionNotes).HasMaxLength(1000);
            entity.HasIndex(x => new { x.AcademyId, x.Status, x.StartDate });
        });
        modelBuilder.Entity<MakeupClass>(entity =>
        {
            entity.Property(x => x.Venue).HasMaxLength(250);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => new { x.AcademyId, x.StartUtc });
        });
    }
}
