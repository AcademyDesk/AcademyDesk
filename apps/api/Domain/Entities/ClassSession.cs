namespace AcademyDesk.Api.Domain.Entities;

public sealed class ClassSession : AcademyEntity
{
    public Guid BatchId { get; set; }
    public Guid? TeacherId { get; set; }
    // Null means the class predates assignment tracking.
    public bool? FollowsBatchTeacher { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string DeliveryMode { get; set; } = "InPerson";
    public string? RoomName { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? TeacherAttendanceStatus { get; set; }
    public DateTime? TeacherAttendanceMarkedAtUtc { get; set; }
}
