namespace AcademyDesk.Api.Domain.Entities;

// Identity lives in a separate EF context. UserId is the authenticated account,
// not a person ID; two accounts linked to one person have independent receipts.
public sealed class NotificationReadReceipt
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public Guid AcademyId { get; set; }
    public DateTime ReadAtUtc { get; set; }
}
