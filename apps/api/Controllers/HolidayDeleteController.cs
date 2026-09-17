using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/holidays")]
public sealed class HolidayDeleteController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpDelete("{holidayId:guid}")]
    public async Task<ActionResult> Delete(Guid academyId, Guid holidayId, CancellationToken token)
    {
        var holiday = await db.AcademyHolidays.SingleOrDefaultAsync(x => x.Id == holidayId && x.AcademyId == academyId, token);
        if (holiday is null) return NotFound();
        db.AcademyHolidays.Remove(holiday);
        await db.SaveChangesAsync(token);
        return NoContent();
    }
}
