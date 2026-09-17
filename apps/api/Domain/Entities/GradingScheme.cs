namespace AcademyDesk.Api.Domain.Entities;
public sealed class GradingScheme:AcademyEntity{public required string Name{get;set;}public decimal PassingPercent{get;set;}public string BandsJson{get;set;}="[]";public bool IsActive{get;set;}=true;}
