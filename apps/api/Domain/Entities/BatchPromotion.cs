namespace AcademyDesk.Api.Domain.Entities;
public sealed class BatchPromotion:AcademyEntity{public Guid StudentId{get;set;}public Guid SourceBatchId{get;set;}public Guid TargetBatchId{get;set;}public DateOnly EffectiveDate{get;set;}public string Status{get;set;}="Pending";public string? Notes{get;set;}}
