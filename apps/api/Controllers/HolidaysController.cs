using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
[ApiController][Route("api/academies/{academyId:guid}/holidays")]
public sealed class HolidaysController(AcademyDeskDbContext db):ControllerBase
{
 [HttpGet] public async Task<ActionResult<IReadOnlyList<AcademyHoliday>>> List(Guid academyId,CancellationToken t)=>Ok(await db.AcademyHolidays.Where(x=>x.AcademyId==academyId).OrderBy(x=>x.HolidayDate).ToListAsync(t));
 [HttpPost] public async Task<ActionResult<AcademyHoliday>> Create(Guid academyId,AcademyHoliday x,CancellationToken t){x.Id=Guid.NewGuid();x.AcademyId=academyId;db.Add(x);await db.SaveChangesAsync(t);return Ok(x);}
 [HttpPost("india-2026-defaults")] public async Task<ActionResult> India(Guid academyId,CancellationToken t){var h=new[]{("Republic Day",1,26),("Id-ul-Fitr",3,21),("Mahavir Jayanti",3,31),("Good Friday",4,3),("Buddha Purnima",5,1),("Id-ul-Zuha",5,27),("Muharram",6,26),("Independence Day",8,15),("Id-e-Milad",8,26),("Gandhi Jayanti",10,2),("Dussehra",10,20),("Diwali",11,8),("Guru Nanak Jayanti",11,24),("Christmas Day",12,25)};var e=await db.AcademyHolidays.Where(x=>x.AcademyId==academyId).Select(x=>x.HolidayDate).ToListAsync(t);db.AddRange(h.Where(x=>!e.Contains(new DateOnly(2026,x.Item2,x.Item3))).Select(x=>new AcademyHoliday{AcademyId=academyId,Name=x.Item1,HolidayDate=new DateOnly(2026,x.Item2,x.Item3),Scope="National",StateOrUt="All India"}));await db.SaveChangesAsync(t);return Ok();}
 [HttpDelete("{id:guid}")] public async Task<ActionResult> Delete(Guid academyId,Guid id,CancellationToken t){var x=await db.AcademyHolidays.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();db.Remove(x);await db.SaveChangesAsync(t);return NoContent();}
}
