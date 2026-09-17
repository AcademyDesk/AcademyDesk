namespace AcademyDesk.Api.Domain.Entities;
public sealed class LessonPlan:AcademyEntity{public Guid BatchId{get;set;}public Guid? CourseModuleId{get;set;}public Guid? ClassSessionId{get;set;}public required string Title{get;set;}public string? Objectives{get;set;}public string Status{get;set;}="Planned";}
