using AcademyDesk.Api.Data;using AcademyDesk.Api.Domain.Entities;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
[ApiController][Route("api/academies/{academyId:guid}/holidays")]
public sealed class HolidaysController(AcademyDeskDbContext db):ControllerBase{[HttpGet]public async Task<ActionResult<IReadOnlyList<AcademyHoliday>>>List(Guid academyId,CancellationToken t)=>Ok(await db.AcademyHolidays.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderBy(x=>x.HolidayDate).ToListAsync(t));[HttpPost]public async Task<ActionResult<AcademyHoliday>>Create(Guid academyId,AcademyHoliday x,CancellationToken t){x.Id=Guid.NewGuid();x.AcademyId=academyId;db.AcademyHolidays.Add(x);await db.SaveChangesAsync(t);return Ok(x);}}
