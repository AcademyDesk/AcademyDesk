namespace AcademyDesk.Api.Domain.Entities;
public sealed class CourseModule:AcademyEntity{public Guid CourseId{get;set;}public required string Title{get;set;}public string? Description{get;set;}public int Sequence{get;set;}public bool IsPublished{get;set;}=true;}
