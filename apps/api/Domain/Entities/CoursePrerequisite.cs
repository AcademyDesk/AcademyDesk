namespace AcademyDesk.Api.Domain.Entities;
public sealed class CoursePrerequisite:AcademyEntity{public Guid CourseId{get;set;}public Guid RequiredCourseId{get;set;}public bool MustBeCompleted{get;set;}=true;}
