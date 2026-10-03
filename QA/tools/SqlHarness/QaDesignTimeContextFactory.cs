using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

// Offline migration scaffolding only. Does not start Program, resolve normal
// configuration, bootstrap users or connect to any development/Azure database.
public sealed class QaDesignTimeContextFactory : IDesignTimeDbContextFactory<AcademyDeskDbContext>
{
    public AcademyDeskDbContext CreateDbContext(string[] args)
    {
        if (!args.Contains("--qa-design-time")) throw new InvalidOperationException("Offline QA design-time flag is required.");
        return new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=AcademyDesk_Offline_Model;Integrated Security=true;Connect Timeout=1").Options);
    }
}
