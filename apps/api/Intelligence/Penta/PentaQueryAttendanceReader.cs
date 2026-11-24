using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaQueryAttendance(string Status, DateTime MarkedAtUtc, string Batch)
{
    private static readonly string[] Statuses = ["Present", "Absent", "Late", "Excused", "Online"];
    public static string? CanonicalStatus(string? value) => Statuses.FirstOrDefault(x => string.Equals(x,value,StringComparison.OrdinalIgnoreCase));
    public static bool ValidFilter(string key,string value) => key switch {
        "name" or "batch" or "teacher" => true, "status" => CanonicalStatus(value)==value,
        "from" or "through" => PentaQueryCalendar.ValidFilter(key,value), _ => false };
    public static bool Valid(PentaQueryAttendance? value) => value is not null && CanonicalStatus(value.Status)==value.Status &&
        value.MarkedAtUtc.Kind==DateTimeKind.Utc && value.MarkedAtUtc> DateTime.MinValue &&
        value.Batch is { Length: > 0 and <= 240 } && !value.Batch.Any(char.IsControl);
}

internal static class PentaQueryAttendanceReader
{
    internal static async Task<PentaQueryDecision> ReadAsync(AcademyDeskDbContext db,TimeProvider clock,Guid academy,
        Dictionary<string,string> filters,Guid? selected,CancellationToken token) {
        var zone=await db.Academies.AsNoTracking().Where(x=>x.Id==academy).Select(x=>x.TimeZone).SingleOrDefaultAsync(token);
        var requested=new Dictionary<string,string>(filters);
        if(!requested.ContainsKey("from"))requested["from"]="Today";
        if(!PentaQueryCalendar.Window(zone,requested,clock.GetUtcNow(),out var scope,out var fromUtc,out var toUtc))
            return new("CLARIFICATION_REQUIRED","Give an exact class date or a range of at most 31 days. The academy time zone and date range must be verifiable.",PentaQueryContext.Empty);
        var query=from mark in db.AttendanceRecords.AsNoTracking()
            join session in db.ClassSessions.AsNoTracking() on mark.ClassSessionId equals session.Id
            join batch in db.Batches.AsNoTracking() on session.BatchId equals batch.Id
            join student in db.Students.AsNoTracking() on mark.StudentId equals student.Id
            where mark.AcademyId==academy && session.AcademyId==academy && batch.AcademyId==academy && student.AcademyId==academy &&
                session.StartUtc>=fromUtc && session.StartUtc<toUtc &&
                (session.TeacherId==null || db.Teachers.Any(t=>t.Id==session.TeacherId && t.AcademyId==academy))
            select new { mark,session,batch,student };
        if(selected is {} id)query=query.Where(x=>x.mark.Id==id);
        if(filters.TryGetValue("name",out var name))query=query.Where(x=>(x.student.FirstName+" "+x.student.LastName).Contains(name)||x.student.StudentNumber!=null && x.student.StudentNumber.Contains(name));
        if(filters.TryGetValue("batch",out var batchName))query=query.Where(x=>x.batch.Name.Contains(batchName)||x.batch.BatchCode!=null && x.batch.BatchCode.Contains(batchName));
        if(filters.TryGetValue("teacher",out var teacher))query=query.Where(x=>db.Teachers.Any(t=>t.AcademyId==academy && t.Id==x.session.TeacherId && (t.FirstName+" "+t.LastName).Contains(teacher)));
        // Existing Manual writes preserve case; compare canonical allowed values
        // case-insensitively without changing stored marks or inventing absences.
        if(filters.TryGetValue("status",out var status))query=query.Where(x=>x.mark.Status.ToUpper()==status.ToUpper());
        var values=await query.OrderBy(x=>x.session.StartUtc).ThenBy(x=>x.mark.Id).Take(11).Select(x=>new {
            x.mark.Id,x.mark.Status,x.mark.MarkedAtUtc,x.student.FirstName,x.student.LastName,x.student.StudentNumber,x.student.IsActive,
            Batch=x.batch.Name,x.session.StartUtc,x.session.EndUtc,x.session.DeliveryMode,ClassStatus=x.session.Status,
            Teacher=db.Teachers.Where(t=>t.AcademyId==academy && t.Id==x.session.TeacherId).Select(t=>t.FirstName+" "+t.LastName).FirstOrDefault()
        }).ToArrayAsync(token);
        if(selected is not null && values.Length!=1)return new("CLARIFICATION_REQUIRED","That recorded attendance entry is no longer available. Search again.",PentaQueryContext.Empty);
        var rows=values.Take(10).Select(x=>new PentaQueryRecord(x.Id,Clip(x.FirstName+" "+x.LastName,240),
            string.IsNullOrWhiteSpace(x.StudentNumber)?null:Clip(x.StudentNumber,100),x.IsActive,[],"/attendance",
            Schedule:new(DateTime.SpecifyKind(x.StartUtc,DateTimeKind.Utc),DateTime.SpecifyKind(x.EndUtc,DateTimeKind.Utc),x.DeliveryMode,x.ClassStatus,
                string.IsNullOrWhiteSpace(x.Teacher)?null:Clip(x.Teacher,240)),
            Attendance:new(PentaQueryAttendance.CanonicalStatus(x.Status)??"",DateTime.SpecifyKind(x.MarkedAtUtc,DateTimeKind.Utc),Clip(x.Batch,240)))).ToArray();
        if(rows.Any(x=>!PentaQueryAttendance.Valid(x.Attendance)||!PentaQueryCalendar.ValidSchedule(x.Schedule,scope)))
            return new("ERROR","Some stored attendance details could not be verified. Use Manual Attendance; no marks were changed.",PentaQueryContext.Empty);
        var pinned=new Dictionary<string,string>(filters) { ["from"]=scope!.From,["through"]=scope.Through };
        var context=new PentaQueryContext("attendance",pinned,rows.Select(x=>x.Id).ToArray());
        var prefix=rows.Length==0?"No matching stored student attendance records were found":values.Length>10?"Showing the first 10 stored student attendance entries; more exist":"Here are the matching stored student attendance entries";
        return new("RESULT",$"{prefix}, for classes starting {scope.From} through {scope.Through} in {scope.TimeZone}. No marks were changed.",context,
            new("attendance",rows,values.Length>10,clock.GetUtcNow().UtcDateTime,CalendarScope:scope,AttendanceScope:PentaQueryContract.AttendanceScope));
    }
    private static string Clip(string text,int max)=>text.Length>max?text[..max]:text;
}
