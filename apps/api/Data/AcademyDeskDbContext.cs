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
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<BatchPromotion> BatchPromotions => Set<BatchPromotion>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<FeePlan> FeePlans => Set<FeePlan>();
    public DbSet<StudentFeeArrangement> StudentFeeArrangements => Set<StudentFeeArrangement>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<FinanceAdjustment> FinanceAdjustments => Set<FinanceAdjustment>();
    public DbSet<AcademyFinanceSettings> AcademyFinanceSettings => Set<AcademyFinanceSettings>();
    public DbSet<PersonDocument> PersonDocuments => Set<PersonDocument>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<AdminWorkItem> AdminWorkItems => Set<AdminWorkItem>();
    public DbSet<AccessReview> AccessReviews => Set<AccessReview>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MusicPiece> MusicPieces => Set<MusicPiece>();
    public DbSet<StudentMusicProgress> StudentMusicProgress => Set<StudentMusicProgress>();
    public DbSet<AcademyEvent> AcademyEvents => Set<AcademyEvent>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<MakeupClass> MakeupClasses => Set<MakeupClass>();
    public DbSet<LearningResource> LearningResources => Set<LearningResource>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
    public DbSet<GradingScheme> GradingSchemes => Set<GradingScheme>();
    public DbSet<LessonPlan> LessonPlans => Set<LessonPlan>();
    public DbSet<CommunicationChannel> CommunicationChannels => Set<CommunicationChannel>();
    public DbSet<CommunicationTemplate> CommunicationTemplates => Set<CommunicationTemplate>();
    public DbSet<CommunicationPreference> CommunicationPreferences => Set<CommunicationPreference>();
    public DbSet<PracticeLog> PracticeLogs => Set<PracticeLog>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<AcademyHoliday> AcademyHolidays => Set<AcademyHoliday>();
    public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();
    public DbSet<PlatformAuditEntry> PlatformAuditEntries => Set<PlatformAuditEntry>();
    public DbSet<PlatformSupportCase> PlatformSupportCases => Set<PlatformSupportCase>();
    public DbSet<PlatformBillingInvoice> PlatformBillingInvoices => Set<PlatformBillingInvoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Academy>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(250);
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.TimeZone).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SubscriptionPlan).HasMaxLength(50).HasDefaultValue("Trial").IsRequired();
            entity.Property(x => x.SubscriptionStatus).HasMaxLength(50).HasDefaultValue("Trial").IsRequired();
            entity.Property(x => x.StudentLimit).HasDefaultValue(100);
            entity.Property(x => x.StaffLimit).HasDefaultValue(10);
            entity.Property(x => x.EnabledModulesJson).HasMaxLength(2000).HasDefaultValue("[\"Core\"]").IsRequired();
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
            entity.Property(x => x.StudentNumber).HasMaxLength(50);
            entity.Property(x => x.PreferredName).HasMaxLength(120);
            entity.Property(x => x.Gender).HasMaxLength(50);
            entity.Property(x => x.AddressLine1).HasMaxLength(240);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.State).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(30);
            entity.Property(x => x.EmergencyContactName).HasMaxLength(160);
            entity.Property(x => x.EmergencyContactPhone).HasMaxLength(30);
            entity.Property(x => x.MedicalOrAccessibilityNotes).HasMaxLength(4000);
            entity.Property(x => x.AdminNotes).HasMaxLength(4000);
            entity.Property(x => x.AdmissionFeeAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.AcademyId, x.LastName, x.FirstName });
            entity.HasIndex(x => new { x.AcademyId, x.Email });
            entity.HasIndex(x => new { x.AcademyId, x.StudentNumber }).IsUnique().HasFilter("[StudentNumber] IS NOT NULL");
        });

        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.PreferredName).HasMaxLength(120);
            entity.Property(x => x.AddressLine1).HasMaxLength(240);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.State).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(30);
            entity.Property(x => x.PreferredLanguage).HasMaxLength(80);
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
            entity.Property(x => x.EmployeeCode).HasMaxLength(50);
            entity.Property(x => x.PreferredName).HasMaxLength(120);
            entity.Property(x => x.EmploymentType).HasMaxLength(50);
            entity.Property(x => x.Qualifications).HasMaxLength(1000);
            entity.Property(x => x.AddressLine1).HasMaxLength(240);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.State).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(30);
            entity.Property(x => x.EmergencyContactName).HasMaxLength(160);
            entity.Property(x => x.EmergencyContactPhone).HasMaxLength(30);
            entity.Property(x => x.AdminNotes).HasMaxLength(4000);
            entity.HasIndex(x => new { x.AcademyId, x.LastName, x.FirstName });
            entity.HasIndex(x => new { x.AcademyId, x.EmployeeCode }).IsUnique().HasFilter("[EmployeeCode] IS NOT NULL");
        });

        modelBuilder.Entity<ProgramCourse>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CourseCode).HasMaxLength(50);
            entity.Property(x => x.AcademyType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.SubjectArea).HasMaxLength(120);
            entity.Property(x => x.Level).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DeliveryMode).HasMaxLength(30);
            entity.Property(x => x.Prerequisites).HasMaxLength(2000);
            entity.Property(x => x.LearningOutcomes).HasMaxLength(4000);
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.AcademyId, x.CourseCode }).IsUnique().HasFilter("[CourseCode] IS NOT NULL");
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BatchCode).HasMaxLength(50);
            entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ClassType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.MeetingDaysJson).HasMaxLength(500);
            entity.Property(x => x.MeetingLink).HasMaxLength(1000);
            entity.Property(x => x.MeetingPattern).HasMaxLength(240);
            entity.Property(x => x.RoomName).HasMaxLength(120);
            entity.Property(x => x.EnrollmentStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.AdminNotes).HasMaxLength(4000);
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.AcademyId, x.BatchCode }).IsUnique().HasFilter("[BatchCode] IS NOT NULL");
        });

        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<AcademicTerm>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.AcademicYearId, x.Name }).IsUnique();
            entity.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.BatchId, x.Status });
        });

        modelBuilder.Entity<FinanceAdjustment>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Type).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ApprovalNotes).HasMaxLength(2000);
            entity.HasIndex(x => new { x.AcademyId, x.InvoiceId, x.Status });
        });
        modelBuilder.Entity<AcademyFinanceSettings>(entity =>
        {
            entity.Property(x => x.TaxRegistrationNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.TaxLabel).HasMaxLength(30).IsRequired();
            entity.Property(x => x.TaxRatePercent).HasPrecision(6, 3);
            entity.HasIndex(x => x.AcademyId).IsUnique();
        });
        modelBuilder.Entity<PersonDocument>(entity => { entity.Property(x => x.DocumentType).HasMaxLength(80).IsRequired(); entity.Property(x => x.FileName).HasMaxLength(260).IsRequired(); entity.Property(x => x.SecureReference).HasMaxLength(1000); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.Property(x => x.Visibility).HasMaxLength(30).IsRequired(); entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.Status }); });
        modelBuilder.Entity<ConsentRecord>(entity => { entity.Property(x => x.ConsentType).HasMaxLength(100).IsRequired(); entity.Property(x => x.EvidenceReference).HasMaxLength(1000); entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.ConsentType }); });
        modelBuilder.Entity<AdminWorkItem>(entity => { entity.Property(x => x.Type).HasMaxLength(60).IsRequired(); entity.Property(x => x.Title).HasMaxLength(240).IsRequired(); entity.Property(x => x.Description).HasMaxLength(4000); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.Property(x => x.Priority).HasMaxLength(20).IsRequired(); entity.Property(x => x.EntityType).HasMaxLength(80); entity.HasIndex(x => new { x.AcademyId, x.Status, x.DueAtUtc }); });

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
            entity.Property(x => x.MaxScore).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.AcademyId, x.BatchId, x.ScheduledAtUtc });
        });

        modelBuilder.Entity<AssessmentResult>(entity =>
        {
            entity.Property(x => x.Score).HasPrecision(10, 2);
            entity.Property(x => x.Grade).HasMaxLength(30);
            entity.Property(x => x.Remarks).HasMaxLength(1000);
            entity.HasIndex(x => new { x.AcademyId, x.AssessmentId, x.StudentId }).IsUnique();
        });

        modelBuilder.Entity<FeePlan>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Frequency).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<StudentFeeArrangement>(entity =>
        {
            entity.Property(x => x.SubjectName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Frequency).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.IsActive });
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
            entity.Property(x => x.VariablesJson).HasMaxLength(8000);
            entity.Property(x => x.FailureReason).HasMaxLength(1000);
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
            entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Venue).HasMaxLength(250);
            entity.Property(x => x.MeetingLink).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => new { x.AcademyId, x.StartUtc });
        });
        modelBuilder.Entity<LearningResource>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Url).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.BatchId, x.IsPublished });
        });
        modelBuilder.Entity<CourseModule>(entity=>{entity.Property(x=>x.Title).HasMaxLength(250).IsRequired();entity.Property(x=>x.Description).HasMaxLength(2000);entity.HasIndex(x=>new{x.AcademyId,x.CourseId,x.Sequence}).IsUnique();});
        modelBuilder.Entity<LessonPlan>(entity=>{entity.Property(x=>x.Title).HasMaxLength(250).IsRequired();entity.Property(x=>x.Objectives).HasMaxLength(2000);entity.Property(x=>x.Status).HasMaxLength(30).IsRequired();entity.HasIndex(x=>new{x.AcademyId,x.BatchId,x.ClassSessionId});});
        modelBuilder.Entity<CommunicationChannel>(entity =>
        {
            entity.Property(x => x.Channel).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.SenderName).HasMaxLength(200);
            entity.Property(x => x.SenderAddress).HasMaxLength(320);
            entity.Property(x => x.ReplyToAddress).HasMaxLength(320);
            entity.Property(x => x.PhoneNumber).HasMaxLength(30);
            entity.Property(x => x.ExternalAccountReference).HasMaxLength(300);
            entity.HasIndex(x => new { x.AcademyId, x.Channel }).IsUnique();
        });
        modelBuilder.Entity<CommunicationTemplate>(entity =>
        {
            entity.Property(x => x.Channel).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TemplateKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(30).IsRequired();
            entity.Property(x => x.TemplateGroup).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Language).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ProviderTemplateName).HasMaxLength(200);
            entity.Property(x => x.Subject).HasMaxLength(250);
            entity.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.Channel, x.TemplateKey }).IsUnique();
        });
        modelBuilder.Entity<CommunicationPreference>(entity =>
        {
            entity.Property(x => x.RecipientType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => new { x.AcademyId, x.RecipientType, x.RecipientId }).IsUnique();
        });
        modelBuilder.Entity<PracticeLog>(entity => { entity.Property(x => x.FocusArea).HasMaxLength(250); entity.Property(x => x.Notes).HasMaxLength(2000); entity.Property(x => x.TeacherFeedback).HasMaxLength(2000); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.PracticeDate }); });
        modelBuilder.Entity<AssignmentSubmission>(entity => { entity.Property(x => x.ResponseText).HasMaxLength(4000); entity.Property(x => x.TeacherFeedback).HasMaxLength(2000); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.HasIndex(x => new { x.AcademyId, x.AssignmentId, x.StudentId }).IsUnique(); });
        modelBuilder.Entity<AcademyHoliday>(entity => { entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); entity.Property(x => x.Notes).HasMaxLength(1000); entity.Property(x => x.Scope).HasMaxLength(30).IsRequired(); entity.Property(x => x.StateOrUt).HasMaxLength(80); entity.HasIndex(x => new { x.AcademyId, x.HolidayDate }).IsUnique(); });
        modelBuilder.Entity<PlatformSettings>(entity => { entity.Property(x => x.PlatformName).HasMaxLength(200).IsRequired(); entity.Property(x => x.SupportEmail).HasMaxLength(320); entity.Property(x => x.DefaultCurrency).HasMaxLength(3).IsRequired(); entity.Property(x => x.StatusMessage).HasMaxLength(1000); });
        modelBuilder.Entity<PlatformAuditEntry>(entity => { entity.Property(x => x.ActorName).HasMaxLength(200).IsRequired(); entity.Property(x => x.Action).HasMaxLength(100).IsRequired(); entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired(); entity.Property(x => x.MetadataJson).HasMaxLength(8000); entity.HasIndex(x => x.OccurredAtUtc); });
        modelBuilder.Entity<PlatformSupportCase>(entity => { entity.Property(x => x.Subject).HasMaxLength(250).IsRequired(); entity.Property(x => x.Priority).HasMaxLength(30).IsRequired(); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.Property(x => x.Description).HasMaxLength(4000); entity.HasIndex(x => new { x.AcademyId, x.Status, x.CreatedAtUtc }); });
        modelBuilder.Entity<PlatformBillingInvoice>(entity => { entity.Property(x => x.InvoiceNumber).HasMaxLength(60).IsRequired(); entity.Property(x => x.Amount).HasPrecision(18, 2); entity.Property(x => x.Currency).HasMaxLength(3).IsRequired(); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.HasIndex(x => x.InvoiceNumber).IsUnique(); entity.HasIndex(x => new { x.AcademyId, x.Status, x.DueDate }); });
    }
}
