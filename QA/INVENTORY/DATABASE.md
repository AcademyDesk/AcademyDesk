# Application database sets and fields

SQL Server mappings from DbContext and EF model snapshot. Actual deployed schema is not introspected. EntityBase/AcademyEntity inherited fields and Identity tables are documented in the main inventory.

## Academy → Academies

Source: apps/api/Domain/Entities/Academy.cs; DbSet: Academies

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| LegalName | string? | yes |
| CountryCode | string | no |
| TimeZone | string | no |
| IsActive | bool | no |
| SubscriptionPlan | string | no |
| SubscriptionStatus | string | no |
| SubscriptionEndsAtUtc | DateTime? | yes |
| StudentLimit | int | no |
| StaffLimit | int | no |
| EnabledModulesJson | string | no |
| CertificateLogoUrl | string? | yes |
| CertificateAccentColor | string? | yes |
| CertificateSignatoryName | string? | yes |
| Branches | List<Branch> | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); |
| entity.Property(x => x.LegalName).HasMaxLength(250); |
| entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired(); |
| entity.Property(x => x.TimeZone).HasMaxLength(100).IsRequired(); |
| entity.Property(x => x.SubscriptionPlan).HasMaxLength(50).HasDefaultValue("Trial").IsRequired(); |
| entity.Property(x => x.SubscriptionStatus).HasMaxLength(50).HasDefaultValue("Trial").IsRequired(); |
| entity.Property(x => x.StudentLimit).HasDefaultValue(100); |
| entity.Property(x => x.StaffLimit).HasDefaultValue(10); |
| entity.Property(x => x.EnabledModulesJson).HasMaxLength(2000).HasDefaultValue("[\"Core\"]").IsRequired(); |

## Branch → Branches

Source: apps/api/Domain/Entities/Branch.cs; DbSet: Branches

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| AddressLine1 | string? | yes |
| City | string? | yes |
| State | string? | yes |
| PostalCode | string? | yes |
| IsActive | bool | no |
| Academy | Academy? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); |
| entity.Property(x => x.City).HasMaxLength(120); |
| entity.Property(x => x.State).HasMaxLength(120); |
| entity.Property(x => x.PostalCode).HasMaxLength(20); |
| entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique(); |
| entity.HasOne(x => x.Academy) |
| .WithMany(x => x.Branches) |
| .HasForeignKey(x => x.AcademyId) |
| .OnDelete(DeleteBehavior.Cascade); |

## Lead → Leads

Source: apps/api/Domain/Entities/Lead.cs; DbSet: Leads

| Property | Type | Nullable annotation |
| --- | --- | --- |
| FullName | string | no |
| Email | string? | yes |
| Phone | string? | yes |
| DateOfBirth | DateOnly? | yes |
| ParentName | string? | yes |
| ProgramInterest | string? | yes |
| Source | string | no |
| Stage | string | no |
| BranchId | Guid? | yes |
| AssignedTeacherId | Guid? | yes |
| FollowUpAtUtc | DateTime? | yes |
| Notes | string? | yes |
| ConvertedStudentId | Guid? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.FullName).HasMaxLength(240).IsRequired(); |
| entity.Property(x => x.Email).HasMaxLength(320); |
| entity.Property(x => x.Phone).HasMaxLength(30); |
| entity.Property(x => x.ParentName).HasMaxLength(240); |
| entity.Property(x => x.ProgramInterest).HasMaxLength(200); |
| entity.Property(x => x.Source).HasMaxLength(50).IsRequired(); |
| entity.Property(x => x.Stage).HasMaxLength(50).IsRequired(); |
| entity.Property(x => x.Notes).HasMaxLength(4000); |
| entity.HasIndex(x => new { x.AcademyId, x.Stage, x.FollowUpAtUtc }); |
| entity.HasIndex(x => new { x.AcademyId, x.Email }); |

## SalesCampaign → SalesCampaigns

Source: apps/api/Domain/Entities/SalesCampaign.cs; DbSet: SalesCampaigns

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| Channel | string | no |
| StartDate | DateOnly | no |
| EndDate | DateOnly? | yes |
| Budget | decimal | no |
| Status | string | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); |
| entity.Property(x => x.Channel).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Budget).HasPrecision(18, 2); |
| entity.HasIndex(x => new { x.AcademyId, x.StartDate }); |

## TrialClassBooking → TrialClassBookings

Source: apps/api/Domain/Entities/TrialClassBooking.cs; DbSet: TrialClassBookings

| Property | Type | Nullable annotation |
| --- | --- | --- |
| LeadId | Guid | no |
| BatchId | Guid? | yes |
| TeacherId | Guid? | yes |
| ScheduledAtUtc | DateTime | no |
| Status | string | no |
| Notes | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Notes).HasMaxLength(2000); |
| entity.HasIndex(x => new { x.AcademyId, x.ScheduledAtUtc }); |
| entity.HasIndex(x => new { x.AcademyId, x.LeadId }); |

## Student → Students

Source: apps/api/Domain/Entities/Student.cs; DbSet: Students

| Property | Type | Nullable annotation |
| --- | --- | --- |
| FirstName | string | no |
| LastName | string | no |
| StudentNumber | string? | yes |
| PreferredName | string? | yes |
| Gender | string? | yes |
| DateOfBirth | DateOnly? | yes |
| AdmissionDate | DateOnly? | yes |
| AdmissionFeeAmount | decimal? | yes |
| AdmissionFeeDueDate | DateOnly? | yes |
| Email | string? | yes |
| Phone | string? | yes |
| AddressLine1 | string? | yes |
| City | string? | yes |
| State | string? | yes |
| PostalCode | string? | yes |
| EmergencyContactName | string? | yes |
| EmergencyContactPhone | string? | yes |
| MedicalOrAccessibilityNotes | string? | yes |
| AdminNotes | string? | yes |
| BranchId | Guid? | yes |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.FirstName).HasMaxLength(120).IsRequired(); |
| entity.Property(x => x.LastName).HasMaxLength(120).IsRequired(); |
| entity.Property(x => x.Email).HasMaxLength(320); |
| entity.Property(x => x.Phone).HasMaxLength(30); |
| entity.Property(x => x.StudentNumber).HasMaxLength(50); |
| entity.Property(x => x.PreferredName).HasMaxLength(120); |
| entity.Property(x => x.Gender).HasMaxLength(50); |
| entity.Property(x => x.AddressLine1).HasMaxLength(240); |
| entity.Property(x => x.City).HasMaxLength(100); |
| entity.Property(x => x.State).HasMaxLength(100); |
| entity.Property(x => x.PostalCode).HasMaxLength(30); |
| entity.Property(x => x.EmergencyContactName).HasMaxLength(160); |
| entity.Property(x => x.EmergencyContactPhone).HasMaxLength(30); |
| entity.Property(x => x.MedicalOrAccessibilityNotes).HasMaxLength(4000); |
| entity.Property(x => x.AdminNotes).HasMaxLength(4000); |
| entity.Property(x => x.AdmissionFeeAmount).HasPrecision(18, 2); |
| entity.HasIndex(x => new { x.AcademyId, x.LastName, x.FirstName }); |
| entity.HasIndex(x => new { x.AcademyId, x.Email }); |
| entity.HasIndex(x => new { x.AcademyId, x.StudentNumber }).IsUnique().HasFilter("[StudentNumber] IS NOT NULL"); |

## Guardian → Guardians

Source: apps/api/Domain/Entities/Guardian.cs; DbSet: Guardians

| Property | Type | Nullable annotation |
| --- | --- | --- |
| FirstName | string | no |
| LastName | string | no |
| PreferredName | string? | yes |
| Email | string? | yes |
| Phone | string? | yes |
| AddressLine1 | string? | yes |
| City | string? | yes |
| State | string? | yes |
| PostalCode | string? | yes |
| PreferredLanguage | string? | yes |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.FirstName).HasMaxLength(120).IsRequired(); |
| entity.Property(x => x.LastName).HasMaxLength(120).IsRequired(); |
| entity.Property(x => x.Email).HasMaxLength(320); |
| entity.Property(x => x.Phone).HasMaxLength(30); |
| entity.Property(x => x.PreferredName).HasMaxLength(120); |
| entity.Property(x => x.AddressLine1).HasMaxLength(240); |
| entity.Property(x => x.City).HasMaxLength(100); |
| entity.Property(x => x.State).HasMaxLength(100); |
| entity.Property(x => x.PostalCode).HasMaxLength(30); |
| entity.Property(x => x.PreferredLanguage).HasMaxLength(80); |
| entity.HasIndex(x => new { x.AcademyId, x.LastName, x.FirstName }); |

## StudentGuardian → StudentGuardians

Source: apps/api/Domain/Entities/StudentGuardian.cs; DbSet: StudentGuardians

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid | no |
| GuardianId | Guid | no |
| Relationship | string? | yes |
| IsPrimary | bool | no |
| CanAccessPortal | bool | no |
| CanViewAcademicProgress | bool | no |
| CanViewFinance | bool | no |
| CanViewDocuments | bool | no |
| CanManageLeave | bool | no |
| AccessGrantedAtUtc | DateTime? | yes |
| AccessRevokedAtUtc | DateTime? | yes |
| Student | Student? | yes |
| Guardian | Guardian? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Relationship).HasMaxLength(50); |
| entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.GuardianId }).IsUnique(); |
| entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade); |
| entity.HasOne(x => x.Guardian).WithMany().HasForeignKey(x => x.GuardianId).OnDelete(DeleteBehavior.Cascade); |

## Teacher → Teachers

Source: apps/api/Domain/Entities/Teacher.cs; DbSet: Teachers

| Property | Type | Nullable annotation |
| --- | --- | --- |
| FirstName | string | no |
| LastName | string | no |
| EmployeeCode | string? | yes |
| PreferredName | string? | yes |
| EmploymentType | string? | yes |
| DateOfBirth | DateOnly? | yes |
| JoiningDate | DateOnly? | yes |
| Email | string? | yes |
| Phone | string? | yes |
| Specialties | string? | yes |
| Qualifications | string? | yes |
| CertificationsJson | string? | yes |
| AvailabilityJson | string? | yes |
| CompensationJson | string? | yes |
| AddressLine1 | string? | yes |
| City | string? | yes |
| State | string? | yes |
| PostalCode | string? | yes |
| EmergencyContactName | string? | yes |
| EmergencyContactPhone | string? | yes |
| AdminNotes | string? | yes |
| BranchId | Guid? | yes |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.FirstName).HasMaxLength(120).IsRequired(); |
| entity.Property(x => x.LastName).HasMaxLength(120).IsRequired(); |
| entity.Property(x => x.Email).HasMaxLength(320); |
| entity.Property(x => x.Phone).HasMaxLength(30); |
| entity.Property(x => x.Specialties).HasMaxLength(500); |
| entity.Property(x => x.EmployeeCode).HasMaxLength(50); |
| entity.Property(x => x.PreferredName).HasMaxLength(120); |
| entity.Property(x => x.EmploymentType).HasMaxLength(50); |
| entity.Property(x => x.Qualifications).HasMaxLength(1000); |
| entity.Property(x => x.AddressLine1).HasMaxLength(240); |
| entity.Property(x => x.City).HasMaxLength(100); |
| entity.Property(x => x.State).HasMaxLength(100); |
| entity.Property(x => x.PostalCode).HasMaxLength(30); |
| entity.Property(x => x.EmergencyContactName).HasMaxLength(160); |
| entity.Property(x => x.EmergencyContactPhone).HasMaxLength(30); |
| entity.Property(x => x.AdminNotes).HasMaxLength(4000); |
| entity.HasIndex(x => new { x.AcademyId, x.LastName, x.FirstName }); |
| entity.HasIndex(x => new { x.AcademyId, x.EmployeeCode }).IsUnique().HasFilter("[EmployeeCode] IS NOT NULL"); |

## ProgramCourse → Courses

Source: apps/api/Domain/Entities/ProgramCourse.cs; DbSet: Courses

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| CourseCode | string? | yes |
| AcademyType | string | no |
| SubjectArea | string? | yes |
| Level | string? | yes |
| Description | string? | yes |
| DurationMonths | int? | yes |
| WeeklySessions | int? | yes |
| SessionMinutes | int? | yes |
| MinimumAge | int? | yes |
| MaximumAge | int? | yes |
| DeliveryMode | string? | yes |
| Prerequisites | string? | yes |
| LearningOutcomes | string? | yes |
| IsPublished | bool | no |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); |
| entity.Property(x => x.CourseCode).HasMaxLength(50); |
| entity.Property(x => x.AcademyType).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.SubjectArea).HasMaxLength(120); |
| entity.Property(x => x.Level).HasMaxLength(100); |
| entity.Property(x => x.Description).HasMaxLength(1000); |
| entity.Property(x => x.DeliveryMode).HasMaxLength(30); |
| entity.Property(x => x.Prerequisites).HasMaxLength(2000); |
| entity.Property(x => x.LearningOutcomes).HasMaxLength(4000); |
| entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique(); |
| entity.HasIndex(x => new { x.AcademyId, x.CourseCode }).IsUnique().HasFilter("[CourseCode] IS NOT NULL"); |

## Batch → Batches

Source: apps/api/Domain/Entities/Batch.cs; DbSet: Batches

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| BatchCode | string? | yes |
| CourseId | Guid | no |
| TeacherId | Guid? | yes |
| BranchId | Guid? | yes |
| Capacity | int | no |
| WaitlistCapacity | int | no |
| ClassType | string | no |
| SessionMinutes | int | no |
| SessionsPerWeek | int | no |
| MeetingDaysJson | string? | yes |
| MeetingLink | string? | yes |
| DeliveryMode | string | no |
| MeetingPattern | string? | yes |
| RoomName | string? | yes |
| EnrollmentStatus | string | no |
| AdminNotes | string? | yes |
| StartDate | DateOnly? | yes |
| EndDate | DateOnly? | yes |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); |
| entity.Property(x => x.BatchCode).HasMaxLength(50); |
| entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.ClassType).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.MeetingDaysJson).HasMaxLength(500); |
| entity.Property(x => x.MeetingLink).HasMaxLength(1000); |
| entity.Property(x => x.MeetingPattern).HasMaxLength(240); |
| entity.Property(x => x.RoomName).HasMaxLength(120); |
| entity.Property(x => x.EnrollmentStatus).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.AdminNotes).HasMaxLength(4000); |
| entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique(); |
| entity.HasIndex(x => new { x.AcademyId, x.BatchCode }).IsUnique().HasFilter("[BatchCode] IS NOT NULL"); |

## AcademicYear → AcademicYears

Source: apps/api/Domain/Entities/AcademicYear.cs; DbSet: AcademicYears

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| StartDate | DateOnly | no |
| EndDate | DateOnly | no |
| IsCurrent | bool | no |
| IsClosed | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Name).HasMaxLength(100).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique(); |

## AcademicTerm → AcademicTerms

Source: apps/api/Domain/Entities/AcademicTerm.cs; DbSet: AcademicTerms

| Property | Type | Nullable annotation |
| --- | --- | --- |
| AcademicYearId | Guid | no |
| Name | string | no |
| StartDate | DateOnly | no |
| EndDate | DateOnly | no |
| IsClosed | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Name).HasMaxLength(100).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.AcademicYearId, x.Name }).IsUnique(); |
| entity.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Cascade); |

## Enrollment → Enrollments

Source: apps/api/Domain/Entities/Enrollment.cs; DbSet: Enrollments

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid | no |
| BatchId | Guid | no |
| StartDate | DateOnly | no |
| EndDate | DateOnly? | yes |
| Status | string | no |
| LifecycleReason | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.BatchId, x.Status }); |

## BatchPromotion → BatchPromotions

Source: apps/api/Domain/Entities/BatchPromotion.cs; DbSet: BatchPromotions

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid | no |
| SourceBatchId | Guid | no |
| TargetBatchId | Guid | no |
| EffectiveDate | DateOnly | no |
| Status | string | no |
| Notes | string? | yes |

| Mapping / constraint |
| --- |


## ClassSession → ClassSessions

Source: apps/api/Domain/Entities/ClassSession.cs; DbSet: ClassSessions

| Property | Type | Nullable annotation |
| --- | --- | --- |
| BatchId | Guid | no |
| TeacherId | Guid? | yes |
| BranchId | Guid? | yes |
| StartUtc | DateTime | no |
| EndUtc | DateTime | no |
| DeliveryMode | string | no |
| RoomName | string? | yes |
| Status | string | no |
| TeacherAttendanceStatus | string? | yes |
| TeacherAttendanceMarkedAtUtc | DateTime? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.RoomName).HasMaxLength(120); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.TeacherAttendanceStatus).HasMaxLength(30); |
| entity.HasIndex(x => new { x.AcademyId, x.StartUtc }); |

## AttendanceRecord → AttendanceRecords

Source: apps/api/Domain/Entities/AttendanceRecord.cs; DbSet: AttendanceRecords

| Property | Type | Nullable annotation |
| --- | --- | --- |
| ClassSessionId | Guid | no |
| StudentId | Guid | no |
| Status | string | no |
| MarkedAtUtc | DateTime | no |
| Notes | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Notes).HasMaxLength(500); |
| entity.HasIndex(x => new { x.AcademyId, x.ClassSessionId, x.StudentId }).IsUnique(); |

## Assignment → Assignments

Source: apps/api/Domain/Entities/Assignment.cs; DbSet: Assignments

| Property | Type | Nullable annotation |
| --- | --- | --- |
| BatchId | Guid | no |
| StudentId | Guid? | yes |
| Title | string | no |
| Description | string? | yes |
| DueAtUtc | DateTime? | yes |
| Type | string | no |
| IsPublished | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Title).HasMaxLength(250).IsRequired(); |
| entity.Property(x => x.Description).HasMaxLength(4000); |
| entity.Property(x => x.Type).HasMaxLength(40).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.BatchId, x.DueAtUtc }); |
| entity.HasIndex(x => new { x.AcademyId, x.BatchId, x.StudentId, x.CreatedAtUtc }); |

## Assessment → Assessments

Source: apps/api/Domain/Entities/Assessment.cs; DbSet: Assessments

| Property | Type | Nullable annotation |
| --- | --- | --- |
| BatchId | Guid | no |
| Title | string | no |
| Type | string | no |
| MaxScore | decimal | no |
| GradingSchemeId | Guid? | yes |
| ScheduledAtUtc | DateTime? | yes |
| IsPublished | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Title).HasMaxLength(250).IsRequired(); |
| entity.Property(x => x.Type).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.MaxScore).HasPrecision(10, 2); |
| entity.HasIndex(x => new { x.AcademyId, x.BatchId, x.ScheduledAtUtc }); |

## AssessmentResult → AssessmentResults

Source: apps/api/Domain/Entities/AssessmentResult.cs; DbSet: AssessmentResults

| Property | Type | Nullable annotation |
| --- | --- | --- |
| AssessmentId | Guid | no |
| StudentId | Guid | no |
| Score | decimal | no |
| Grade | string? | yes |
| Remarks | string? | yes |
| IsPublished | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Score).HasPrecision(10, 2); |
| entity.Property(x => x.Grade).HasMaxLength(30); |
| entity.Property(x => x.Remarks).HasMaxLength(1000); |
| entity.HasIndex(x => new { x.AcademyId, x.AssessmentId, x.StudentId }).IsUnique(); |

## FeePlan → FeePlans

Source: apps/api/Domain/Entities/FeePlan.cs; DbSet: FeePlans

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| Amount | decimal | no |
| Currency | string | no |
| Frequency | string | no |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); |
| entity.Property(x => x.Amount).HasPrecision(18, 2); |
| entity.Property(x => x.Currency).HasMaxLength(3).IsRequired(); |
| entity.Property(x => x.Frequency).HasMaxLength(30).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique(); |

## StudentFeeArrangement → StudentFeeArrangements

Source: apps/api/Domain/Entities/StudentFeeArrangement.cs; DbSet: StudentFeeArrangements

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid | no |
| CourseId | Guid? | yes |
| SubjectName | string | no |
| Amount | decimal | no |
| Frequency | string | no |
| EffectiveFrom | DateOnly | no |
| EffectiveTo | DateOnly? | yes |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.SubjectName).HasMaxLength(200).IsRequired(); |
| entity.Property(x => x.Amount).HasPrecision(18, 2); |
| entity.Property(x => x.Frequency).HasMaxLength(30).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.IsActive }); |

## Invoice → Invoices

Source: apps/api/Domain/Entities/Invoice.cs; DbSet: Invoices

| Property | Type | Nullable annotation |
| --- | --- | --- |
| InvoiceNumber | string | no |
| StudentId | Guid | no |
| FeePlanId | Guid? | yes |
| TotalAmount | decimal | no |
| AdjustedAmount | decimal | no |
| Currency | string | no |
| IssuedDate | DateOnly | no |
| DueDate | DateOnly | no |
| Status | string | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.InvoiceNumber).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.TotalAmount).HasPrecision(18, 2); |
| entity.Property(x => x.Currency).HasMaxLength(3).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.InvoiceNumber }).IsUnique(); |
| entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.Status }); |

## Payment → Payments

Source: apps/api/Domain/Entities/Payment.cs; DbSet: Payments

| Property | Type | Nullable annotation |
| --- | --- | --- |
| InvoiceId | Guid | no |
| Amount | decimal | no |
| Currency | string | no |
| Method | string | no |
| Status | string | no |
| Reference | string? | yes |
| PaidAtUtc | DateTime | no |
| ReconciledAtUtc | DateTime? | yes |
| ReconciliationReference | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Amount).HasPrecision(18, 2); |
| entity.Property(x => x.Currency).HasMaxLength(3).IsRequired(); |
| entity.Property(x => x.Method).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Reference).HasMaxLength(150); |
| entity.HasIndex(x => new { x.AcademyId, x.InvoiceId, x.PaidAtUtc }); |

## Expense → Expenses

Source: apps/api/Domain/Entities/Expense.cs; DbSet: Expenses

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Description | string | no |
| Amount | decimal | no |
| Currency | string | no |
| Category | string | no |
| BranchId | Guid? | yes |
| ExpenseDate | DateOnly | no |
| Status | string | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Description).HasMaxLength(300).IsRequired(); |
| entity.Property(x => x.Amount).HasPrecision(18, 2); |
| entity.Property(x => x.Currency).HasMaxLength(3).IsRequired(); |
| entity.Property(x => x.Category).HasMaxLength(80).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.ExpenseDate }); |

## PayrollProfile → PayrollProfiles

Source: apps/api/Domain/Entities/PayrollProfile.cs; DbSet: PayrollProfiles

| Property | Type | Nullable annotation |
| --- | --- | --- |
| WorkerType | string | no |
| TeacherId | Guid? | yes |
| StaffUserId | string? | yes |
| WorkerName | string | no |
| PaymentModel | string | no |
| MonthlyAmount | decimal? | yes |
| AmountPerCycle | decimal? | yes |
| SessionsPerCycle | int? | yes |
| EffectiveFrom | DateOnly | no |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.WorkerType).HasMaxLength(20).IsRequired(); |
| entity.Property(x => x.StaffUserId).HasMaxLength(450); |
| entity.Property(x => x.WorkerName).HasMaxLength(240).IsRequired(); |
| entity.Property(x => x.PaymentModel).HasMaxLength(20).IsRequired(); |
| entity.Property(x => x.MonthlyAmount).HasPrecision(18, 2); |
| entity.Property(x => x.AmountPerCycle).HasPrecision(18, 2); |
| entity.HasIndex(x => new { x.AcademyId, x.TeacherId }); |
| entity.HasIndex(x => new { x.AcademyId, x.StaffUserId }); |

## PayrollPayout → PayrollPayouts

Source: apps/api/Domain/Entities/PayrollPayout.cs; DbSet: PayrollPayouts

| Property | Type | Nullable annotation |
| --- | --- | --- |
| PayrollProfileId | Guid | no |
| PayslipNumber | string | no |
| PeriodLabel | string | no |
| SessionsCovered | int? | yes |
| GrossAmount | decimal | no |
| Deductions | decimal | no |
| NetAmount | decimal | no |
| Currency | string | no |
| Status | string | no |
| PaymentMethod | string | no |
| Reference | string? | yes |
| PaidAtUtc | DateTime | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.PayslipNumber).HasMaxLength(60).IsRequired(); |
| entity.Property(x => x.PeriodLabel).HasMaxLength(120).IsRequired(); |
| entity.Property(x => x.GrossAmount).HasPrecision(18, 2); |
| entity.Property(x => x.Deductions).HasPrecision(18, 2); |
| entity.Property(x => x.NetAmount).HasPrecision(18, 2); |
| entity.Property(x => x.Currency).HasMaxLength(3).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Reference).HasMaxLength(150); |
| entity.HasIndex(x => new { x.AcademyId, x.PayslipNumber }).IsUnique(); |
| entity.HasIndex(x => new { x.AcademyId, x.PayrollProfileId, x.PaidAtUtc }); |

## FinanceAdjustment → FinanceAdjustments

Source: apps/api/Domain/Entities/FinanceAdjustment.cs; DbSet: FinanceAdjustments

| Property | Type | Nullable annotation |
| --- | --- | --- |
| InvoiceId | Guid | no |
| Type | string | no |
| Amount | decimal | no |
| Currency | string | no |
| Reason | string | no |
| Status | string | no |
| ApprovedAtUtc | DateTime? | yes |
| ApprovalNotes | string? | yes |
| AppliedAtUtc | DateTime? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Amount).HasPrecision(18, 2); |
| entity.Property(x => x.Type).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Currency).HasMaxLength(3).IsRequired(); |
| entity.Property(x => x.Reason).HasMaxLength(2000).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.ApprovalNotes).HasMaxLength(2000); |
| entity.HasIndex(x => new { x.AcademyId, x.InvoiceId, x.Status }); |

## AcademyFinanceSettings → AcademyFinanceSettings

Source: apps/api/Domain/Entities/AcademyFinanceSettings.cs; DbSet: AcademyFinanceSettings

| Property | Type | Nullable annotation |
| --- | --- | --- |
| TaxRegistrationNumber | string | no |
| TaxLabel | string | no |
| TaxRatePercent | decimal | no |
| DefaultPaymentTermsDays | int | no |
| TaxInclusivePricing | bool | no |
| InvoiceLogoUrl | string? | yes |
| InvoiceAuthorityName | string? | yes |
| InvoiceAuthorityTitle | string? | yes |
| InvoiceSignatureUrl | string? | yes |
| InvoiceTemplateKey | string | no |
| PayslipTemplateKey | string | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.TaxRegistrationNumber).HasMaxLength(50).IsRequired(); |
| entity.Property(x => x.TaxLabel).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.TaxRatePercent).HasPrecision(6, 3); |
| entity.HasIndex(x => x.AcademyId).IsUnique(); |

## PersonDocument → PersonDocuments

Source: apps/api/Domain/Entities/PersonDocument.cs; DbSet: PersonDocuments

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid? | yes |
| GuardianId | Guid? | yes |
| DocumentType | string | no |
| FileName | string | no |
| SecureReference | string? | yes |
| ExpiryDate | DateOnly? | yes |
| ReviewedDate | DateOnly? | yes |
| Status | string | no |
| Visibility | string | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.DocumentType).HasMaxLength(80).IsRequired(); entity.Property(x => x.FileName).HasMaxLength(260).IsRequired(); entity.Property(x => x.SecureReference).HasMaxLength(1000); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.Property(x => x.Visibility).HasMaxLength(30).IsRequired(); entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.Status }); }); |
| modelBuilder.Entity<ConsentRecord>(entity => { entity.Property(x => x.ConsentType).HasMaxLength(100).IsRequired(); entity.Property(x => x.EvidenceReference).HasMaxLength(1000); entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.ConsentType }); }); |
| modelBuilder.Entity<AdminWorkItem>(entity => { entity.Property(x => x.Type).HasMaxLength(60).IsRequired(); entity.Property(x => x.Title).HasMaxLength(240).IsRequired(); entity.Property(x => x.Description).HasMaxLength(4000); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.Property(x => x.Priority).HasMaxLength(20).IsRequired(); entity.Property(x => x.EntityType).HasMaxLength(80); entity.HasIndex(x => new { x.AcademyId, x.Status, x.DueAtUtc }); }); |
| modelBuilder.Entity<ClassSession>(entity => |
| { |
| entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.RoomName).HasMaxLength(120); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.TeacherAttendanceStatus).HasMaxLength(30); |
| entity.HasIndex(x => new { x.AcademyId, x.StartUtc }); |

## ConsentRecord → ConsentRecords

Source: apps/api/Domain/Entities/ConsentRecord.cs; DbSet: ConsentRecords

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid? | yes |
| GuardianId | Guid? | yes |
| ConsentType | string | no |
| Granted | bool | no |
| RecordedAtUtc | DateTime | no |
| WithdrawnAtUtc | DateTime? | yes |
| EvidenceReference | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.ConsentType).HasMaxLength(100).IsRequired(); entity.Property(x => x.EvidenceReference).HasMaxLength(1000); entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.ConsentType }); }); |
| modelBuilder.Entity<AdminWorkItem>(entity => { entity.Property(x => x.Type).HasMaxLength(60).IsRequired(); entity.Property(x => x.Title).HasMaxLength(240).IsRequired(); entity.Property(x => x.Description).HasMaxLength(4000); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.Property(x => x.Priority).HasMaxLength(20).IsRequired(); entity.Property(x => x.EntityType).HasMaxLength(80); entity.HasIndex(x => new { x.AcademyId, x.Status, x.DueAtUtc }); }); |
| modelBuilder.Entity<ClassSession>(entity => |
| { |
| entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.RoomName).HasMaxLength(120); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.TeacherAttendanceStatus).HasMaxLength(30); |
| entity.HasIndex(x => new { x.AcademyId, x.StartUtc }); |

## AdminWorkItem → AdminWorkItems

Source: apps/api/Domain/Entities/AdminWorkItem.cs; DbSet: AdminWorkItems

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Type | string | no |
| Title | string | no |
| Description | string? | yes |
| Status | string | no |
| Priority | string | no |
| EntityType | string? | yes |
| EntityId | Guid? | yes |
| AssignedUserId | Guid? | yes |
| DueAtUtc | DateTime? | yes |
| CompletedAtUtc | DateTime? | yes |
| EscalationStage | string? | yes |
| PromisedPaymentDate | DateOnly? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Type).HasMaxLength(60).IsRequired(); entity.Property(x => x.Title).HasMaxLength(240).IsRequired(); entity.Property(x => x.Description).HasMaxLength(4000); entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); entity.Property(x => x.Priority).HasMaxLength(20).IsRequired(); entity.Property(x => x.EntityType).HasMaxLength(80); entity.HasIndex(x => new { x.AcademyId, x.Status, x.DueAtUtc }); }); |
| modelBuilder.Entity<ClassSession>(entity => |
| { |
| entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.RoomName).HasMaxLength(120); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.TeacherAttendanceStatus).HasMaxLength(30); |
| entity.HasIndex(x => new { x.AcademyId, x.StartUtc }); |

## AccessReview → AccessReviews

Source: apps/api/Domain/Entities/AccessReview.cs; DbSet: AccessReviews

| Property | Type | Nullable annotation |
| --- | --- | --- |
| ReviewedAtUtc | DateTime | no |
| Notes | string? | yes |
| ReviewerUserId | Guid? | yes |

| Mapping / constraint |
| --- |


## Notification → Notifications

Source: apps/api/Domain/Entities/Notification.cs; DbSet: Notifications

| Property | Type | Nullable annotation |
| --- | --- | --- |
| RecipientId | Guid? | yes |
| RecipientType | string | no |
| Title | string | no |
| Message | string | no |
| Channel | string | no |
| Status | string | no |
| TemplateId | Guid? | yes |
| VariablesJson | string? | yes |
| FailureReason | string? | yes |
| AttemptCount | int | no |
| ScheduledAtUtc | DateTime? | yes |
| SentAtUtc | DateTime? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.RecipientType).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.Title).HasMaxLength(250).IsRequired(); |
| entity.Property(x => x.Message).HasMaxLength(4000).IsRequired(); |
| entity.Property(x => x.Channel).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.VariablesJson).HasMaxLength(8000); |
| entity.Property(x => x.FailureReason).HasMaxLength(1000); |
| entity.HasIndex(x => new { x.AcademyId, x.RecipientId, x.CreatedAtUtc }); |

## AuditLog → AuditLogs

Source: apps/api/Domain/Entities/AuditLog.cs; DbSet: AuditLogs

| Property | Type | Nullable annotation |
| --- | --- | --- |
| ActorUserId | Guid? | yes |
| Action | string | no |
| EntityType | string | no |
| EntityId | Guid? | yes |
| MetadataJson | string? | yes |
| IpAddress | string? | yes |
| OccurredAtUtc | DateTime | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Action).HasMaxLength(80).IsRequired(); |
| entity.Property(x => x.EntityType).HasMaxLength(80).IsRequired(); |
| entity.Property(x => x.MetadataJson).HasMaxLength(8000); |
| entity.Property(x => x.IpAddress).HasMaxLength(64); |
| entity.HasIndex(x => new { x.AcademyId, x.OccurredAtUtc }); |
| entity.HasIndex(x => new { x.AcademyId, x.EntityType, x.EntityId }); |

## MusicPiece → MusicPieces

Source: apps/api/Domain/Entities/MusicPiece.cs; DbSet: MusicPieces

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Title | string | no |
| Composer | string? | yes |
| Instrument | string? | yes |
| Genre | string? | yes |
| Difficulty | string | no |
| DurationMinutes | int? | yes |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Title).HasMaxLength(250).IsRequired(); |
| entity.Property(x => x.Composer).HasMaxLength(200); |
| entity.Property(x => x.Instrument).HasMaxLength(100); |
| entity.Property(x => x.Genre).HasMaxLength(100); |
| entity.Property(x => x.Difficulty).HasMaxLength(40).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.Title }); |

## StudentMusicProgress → StudentMusicProgress

Source: apps/api/Domain/Entities/StudentMusicProgress.cs; DbSet: StudentMusicProgress

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid | no |
| MusicPieceId | Guid | no |
| Status | string | no |
| TargetDate | DateOnly? | yes |
| Score | decimal? | yes |
| Notes | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Status).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.Notes).HasMaxLength(2000); |
| entity.Property(x => x.Score).HasPrecision(5, 2); |
| entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.MusicPieceId }).IsUnique(); |

## AcademyEvent → AcademyEvents

Source: apps/api/Domain/Entities/AcademyEvent.cs; DbSet: AcademyEvents

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Title | string | no |
| Type | string | no |
| BranchId | Guid? | yes |
| StartUtc | DateTime | no |
| EndUtc | DateTime | no |
| Venue | string? | yes |
| Capacity | int? | yes |
| Status | string | no |
| Notes | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Title).HasMaxLength(250).IsRequired(); |
| entity.Property(x => x.Type).HasMaxLength(50).IsRequired(); |
| entity.Property(x => x.Venue).HasMaxLength(250); |
| entity.Property(x => x.Status).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.Notes).HasMaxLength(4000); |
| entity.HasIndex(x => new { x.AcademyId, x.StartUtc }); |

## Certificate → Certificates

Source: apps/api/Domain/Entities/Certificate.cs; DbSet: Certificates

| Property | Type | Nullable annotation |
| --- | --- | --- |
| CertificateNumber | string | no |
| StudentId | Guid | no |
| BatchId | Guid? | yes |
| Title | string | no |
| TemplateKey | string | no |
| DesignKey | string | no |
| ArtworkX | int | no |
| ArtworkY | int | no |
| ArtworkSize | int | no |
| VerificationCode | string | no |
| IssuedDate | DateOnly | no |
| Status | string | no |
| Notes | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.CertificateNumber).HasMaxLength(60).IsRequired(); |
| entity.Property(x => x.Title).HasMaxLength(250).IsRequired(); |
| entity.Property(x => x.TemplateKey).HasMaxLength(60).IsRequired(); |
| entity.Property(x => x.DesignKey).HasMaxLength(60).IsRequired(); |
| entity.Property(x => x.ArtworkX).IsRequired(); |
| entity.Property(x => x.ArtworkY).IsRequired(); |
| entity.Property(x => x.ArtworkSize).IsRequired(); |
| entity.Property(x => x.VerificationCode).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Notes).HasMaxLength(2000); |
| entity.HasIndex(x => new { x.AcademyId, x.CertificateNumber }).IsUnique(); |
| entity.HasIndex(x => new { x.AcademyId, x.StudentId, x.IssuedDate }); |

## LeaveRequest → LeaveRequests

Source: apps/api/Domain/Entities/LeaveRequest.cs; DbSet: LeaveRequests

| Property | Type | Nullable annotation |
| --- | --- | --- |
| RequesterType | string | no |
| StudentId | Guid? | yes |
| TeacherId | Guid? | yes |
| StartDate | DateOnly | no |
| EndDate | DateOnly | no |
| Reason | string | no |
| Status | string | no |
| DecisionNotes | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.RequesterType).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Reason).HasMaxLength(2000).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.DecisionNotes).HasMaxLength(1000); |
| entity.HasIndex(x => new { x.AcademyId, x.Status, x.StartDate }); |

## MakeupClass → MakeupClasses

Source: apps/api/Domain/Entities/MakeupClass.cs; DbSet: MakeupClasses

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid | no |
| BatchId | Guid | no |
| TeacherId | Guid? | yes |
| StartUtc | DateTime | no |
| EndUtc | DateTime | no |
| DeliveryMode | string | no |
| Venue | string? | yes |
| MeetingLink | string? | yes |
| UsesNextScheduledClass | bool | no |
| Status | string | no |
| Notes | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.DeliveryMode).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Venue).HasMaxLength(250); |
| entity.Property(x => x.MeetingLink).HasMaxLength(1000); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Notes).HasMaxLength(2000); |
| entity.HasIndex(x => new { x.AcademyId, x.StartUtc }); |

## LearningResource → LearningResources

Source: apps/api/Domain/Entities/LearningResource.cs; DbSet: LearningResources

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Title | string | no |
| Description | string? | yes |
| Type | string | no |
| Url | string | no |
| BatchId | Guid? | yes |
| CourseId | Guid? | yes |
| StudentId | Guid? | yes |
| ClassSessionId | Guid? | yes |
| IsPublished | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Title).HasMaxLength(250).IsRequired(); |
| entity.Property(x => x.Description).HasMaxLength(2000); |
| entity.Property(x => x.Type).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.Url).HasMaxLength(2000).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.BatchId, x.IsPublished }); |
| entity.HasIndex(x => new { x.AcademyId, x.ClassSessionId, x.StudentId }); |

## CourseModule → CourseModules

Source: apps/api/Domain/Entities/CourseModule.cs; DbSet: CourseModules

| Property | Type | Nullable annotation |
| --- | --- | --- |
| CourseId | Guid | no |
| Title | string | no |
| Description | string? | yes |
| Sequence | int | no |
| IsPublished | bool | no |

| Mapping / constraint |
| --- |


## CoursePrerequisite → CoursePrerequisites

Source: apps/api/Domain/Entities/CoursePrerequisite.cs; DbSet: CoursePrerequisites

| Property | Type | Nullable annotation |
| --- | --- | --- |
| CourseId | Guid | no |
| RequiredCourseId | Guid | no |
| MustBeCompleted | bool | no |

| Mapping / constraint |
| --- |


## GradingScheme → GradingSchemes

Source: apps/api/Domain/Entities/GradingScheme.cs; DbSet: GradingSchemes

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| PassingPercent | decimal | no |
| BandsJson | string | no |
| IsActive | bool | no |

| Mapping / constraint |
| --- |


## LessonPlan → LessonPlans

Source: apps/api/Domain/Entities/LessonPlan.cs; DbSet: LessonPlans

| Property | Type | Nullable annotation |
| --- | --- | --- |
| BatchId | Guid | no |
| CourseModuleId | Guid? | yes |
| ClassSessionId | Guid? | yes |
| Title | string | no |
| Objectives | string? | yes |
| Status | string | no |

| Mapping / constraint |
| --- |


## CommunicationChannel → CommunicationChannels

Source: apps/api/Domain/Entities/CommunicationChannel.cs; DbSet: CommunicationChannels

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Channel | string | no |
| Provider | string | no |
| Status | string | no |
| SenderName | string? | yes |
| SenderAddress | string? | yes |
| ReplyToAddress | string? | yes |
| PhoneNumber | string? | yes |
| ExternalAccountReference | string? | yes |
| MessagesEnabled | bool | no |
| HasSecureConnection | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Channel).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Provider).HasMaxLength(60).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.SenderName).HasMaxLength(200); |
| entity.Property(x => x.SenderAddress).HasMaxLength(320); |
| entity.Property(x => x.ReplyToAddress).HasMaxLength(320); |
| entity.Property(x => x.PhoneNumber).HasMaxLength(30); |
| entity.Property(x => x.ExternalAccountReference).HasMaxLength(300); |
| entity.HasIndex(x => new { x.AcademyId, x.Channel }).IsUnique(); |

## CommunicationTemplate → CommunicationTemplates

Source: apps/api/Domain/Entities/CommunicationTemplate.cs; DbSet: CommunicationTemplates

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Channel | string | no |
| Name | string | no |
| TemplateKey | string | no |
| Category | string | no |
| TemplateGroup | string | no |
| Status | string | no |
| Language | string | no |
| ProviderTemplateName | string? | yes |
| Subject | string? | yes |
| Body | string | no |
| IsActive | bool | no |

| Mapping / constraint |
| --- |
| entity.Property(x => x.Channel).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); |
| entity.Property(x => x.TemplateKey).HasMaxLength(100).IsRequired(); |
| entity.Property(x => x.Category).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.TemplateGroup).HasMaxLength(60).IsRequired(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Language).HasMaxLength(20).IsRequired(); |
| entity.Property(x => x.ProviderTemplateName).HasMaxLength(200); |
| entity.Property(x => x.Subject).HasMaxLength(250); |
| entity.Property(x => x.Body).HasMaxLength(4000).IsRequired(); |
| entity.HasIndex(x => new { x.AcademyId, x.Channel, x.TemplateKey }).IsUnique(); |

## CommunicationPreference → CommunicationPreferences

Source: apps/api/Domain/Entities/CommunicationPreference.cs; DbSet: CommunicationPreferences

| Property | Type | Nullable annotation |
| --- | --- | --- |
| RecipientId | Guid | no |
| RecipientType | string | no |
| EmailAllowed | bool | no |
| WhatsAppAllowed | bool | no |
| MarketingAllowed | bool | no |
| EmailOptedInAtUtc | DateTime? | yes |
| WhatsAppOptedInAtUtc | DateTime? | yes |
| OptedOutAtUtc | DateTime? | yes |
| Notes | string? | yes |

| Mapping / constraint |
| --- |
| entity.Property(x => x.RecipientType).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.Notes).HasMaxLength(1000); |
| entity.HasIndex(x => new { x.AcademyId, x.RecipientType, x.RecipientId }).IsUnique(); |

## PracticeLog → PracticeLogs

Source: apps/api/Domain/Entities/PracticeLog.cs; DbSet: PracticeLogs

| Property | Type | Nullable annotation |
| --- | --- | --- |
| StudentId | Guid | no |
| PracticeDate | DateOnly | no |
| MinutesPracticed | int | no |
| FocusArea | string? | yes |
| Notes | string? | yes |
| TeacherFeedback | string? | yes |
| ReviewedAtUtc | DateTime? | yes |
| Status | string | no |

| Mapping / constraint |
| --- |


## AssignmentSubmission → AssignmentSubmissions

Source: apps/api/Domain/Entities/AssignmentSubmission.cs; DbSet: AssignmentSubmissions

| Property | Type | Nullable annotation |
| --- | --- | --- |
| AssignmentId | Guid | no |
| StudentId | Guid | no |
| ResponseText | string? | yes |
| Status | string | no |
| SubmittedAtUtc | DateTime | no |
| TeacherFeedback | string? | yes |

| Mapping / constraint |
| --- |


## AcademyHoliday → AcademyHolidays

Source: apps/api/Domain/Entities/AcademyHoliday.cs; DbSet: AcademyHolidays

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Name | string | no |
| HolidayDate | DateOnly | no |
| Notes | string? | yes |
| Scope | string | no |
| StateOrUt | string? | yes |
| IsClosed | bool | no |

| Mapping / constraint |
| --- |


## PlatformSettings → PlatformSettings

Source: apps/api/Domain/Entities/PlatformSettings.cs; DbSet: PlatformSettings

| Property | Type | Nullable annotation |
| --- | --- | --- |
| PlatformName | string | no |
| SupportEmail | string? | yes |
| DefaultCurrency | string | no |
| DefaultTrialDays | int | no |
| DataRetentionDays | int | no |
| MaintenanceMode | bool | no |
| StatusMessage | string? | yes |

| Mapping / constraint |
| --- |


## PlatformAuditEntry → PlatformAuditEntries

Source: apps/api/Domain/Entities/PlatformAuditEntry.cs; DbSet: PlatformAuditEntries

| Property | Type | Nullable annotation |
| --- | --- | --- |
| ActorUserId | Guid? | yes |
| ActorName | string | no |
| Action | string | no |
| EntityType | string | no |
| EntityId | Guid? | yes |
| MetadataJson | string? | yes |
| OccurredAtUtc | DateTime | no |

| Mapping / constraint |
| --- |


## PlatformSupportCase → PlatformSupportCases

Source: apps/api/Domain/Entities/PlatformSupportCase.cs; DbSet: PlatformSupportCases

| Property | Type | Nullable annotation |
| --- | --- | --- |
| AcademyId | Guid | no |
| Subject | string | no |
| Priority | string | no |
| Status | string | no |
| Description | string? | yes |
| AcademyResponse | string? | yes |
| AcademyRespondedAtUtc | DateTime? | yes |
| AssignedToUserId | Guid? | yes |
| ResolvedAtUtc | DateTime? | yes |

| Mapping / constraint |
| --- |


## PlatformBillingInvoice → PlatformBillingInvoices

Source: apps/api/Domain/Entities/PlatformBillingInvoice.cs; DbSet: PlatformBillingInvoices

| Property | Type | Nullable annotation |
| --- | --- | --- |
| AcademyId | Guid | no |
| InvoiceNumber | string | no |
| Amount | decimal | no |
| Currency | string | no |
| Status | string | no |
| PeriodStart | DateOnly | no |
| PeriodEnd | DateOnly | no |
| DueDate | DateOnly | no |
| PaidAtUtc | DateTime? | yes |
| PaymentReference | string? | yes |
| PaymentSubmittedAtUtc | DateTime? | yes |

| Mapping / constraint |
| --- |


## TenantOnboardingProfile → TenantOnboardingProfiles

Source: apps/api/Domain/Entities/TenantOnboardingProfile.cs; DbSet: TenantOnboardingProfiles

| Property | Type | Nullable annotation |
| --- | --- | --- |
| Status | string | no |
| CurrentSection | string | no |
| PrimaryContactName | string? | yes |
| PrimaryContactRole | string? | yes |
| PrimaryContactEmail | string? | yes |
| PrimaryContactPhone | string? | yes |
| Country | string? | yes |
| State | string? | yes |
| City | string? | yes |
| PostalCode | string? | yes |
| AddressLine1 | string? | yes |
| AddressLine2 | string? | yes |
| BusinessType | string? | yes |
| OperatingSince | string? | yes |
| Website | string? | yes |
| BranchSummary | string? | yes |
| FinanceModel | string? | yes |
| BillingFrequency | string? | yes |
| PaymentCollectionMethods | string? | yes |
| TeacherPaymentModels | string? | yes |
| TeacherCount | int? | yes |
| StudentCount | int? | yes |
| SubjectCount | int? | yes |
| SubjectTypes | string? | yes |
| DeliveryModes | string? | yes |
| ClassRatios | string? | yes |
| BatchAndClassSetup | string? | yes |
| OperationalNotes | string? | yes |
| DocumentsJson | string? | yes |

| Mapping / constraint |
| --- |
| entity.HasIndex(x => x.AcademyId).IsUnique(); |
| entity.Property(x => x.Status).HasMaxLength(30).IsRequired(); |
| entity.Property(x => x.CurrentSection).HasMaxLength(40).IsRequired(); |
| entity.Property(x => x.PrimaryContactName).HasMaxLength(200); |
| entity.Property(x => x.PrimaryContactRole).HasMaxLength(120); |
| entity.Property(x => x.PrimaryContactEmail).HasMaxLength(320); |
| entity.Property(x => x.PrimaryContactPhone).HasMaxLength(40); |
| entity.Property(x => x.Country).HasMaxLength(120); |
| entity.Property(x => x.State).HasMaxLength(120); |
| entity.Property(x => x.City).HasMaxLength(120); |
| entity.Property(x => x.PostalCode).HasMaxLength(24); |
| entity.Property(x => x.AddressLine1).HasMaxLength(300); |
| entity.Property(x => x.AddressLine2).HasMaxLength(300); |
| entity.Property(x => x.BusinessType).HasMaxLength(120); |
| entity.Property(x => x.OperatingSince).HasMaxLength(20); |
| entity.Property(x => x.Website).HasMaxLength(500); |
| entity.Property(x => x.FinanceModel).HasMaxLength(160); |
| entity.Property(x => x.BillingFrequency).HasMaxLength(120); |
| entity.Property(x => x.TeacherPaymentModels).HasMaxLength(300); |
| entity.Property(x => x.DocumentsJson).HasMaxLength(8000); |
