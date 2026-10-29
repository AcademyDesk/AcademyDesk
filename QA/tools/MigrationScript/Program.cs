using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

// Generate historical Up scripts only. No connection is opened; no startup code runs.
if (args.Length != 3 || !Directory.Exists(args[0]) ||
    !System.Text.RegularExpressions.Regex.IsMatch(args[1], @"^[0-9]{14}_[A-Za-z0-9_]+$") ||
    !System.Text.RegularExpressions.Regex.IsMatch(args[2], @"^[0-9]{14}_[A-Za-z0-9_]+$"))
    throw new InvalidOperationException("Existing output directory and two exact migration targets required.");
var unreachable = "Server=127.0.0.1,1;Database=AcademyDesk_QA_NoConnection;Integrated Security=true;Encrypt=false;Connect Timeout=1";
using var application = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(unreachable).Options);
using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(unreachable).Options);
foreach (var item in new (DbContext Context, string Target, string File)[]
{
    (application, args[1], "baseline-application.sql"),
    (identity, args[2], "baseline-identity.sql")
})
{
    if (!item.Context.Database.GetMigrations().Contains(item.Target)) throw new InvalidOperationException("Target is absent from exact candidate assembly.");
    // SQL Server compiles a sqlcmd batch before executing it. Preserve EF's
    // individual command boundaries so ADD COLUMN followed by backfill compiles
    // only after the column exists, while preserving migration transactions.
    var assembly = item.Context.GetService<IMigrationsAssembly>();
    var sqlGenerator = item.Context.GetService<IMigrationsSqlGenerator>();
    var history = item.Context.GetService<IHistoryRepository>();
    var script = new System.Text.StringBuilder(history.GetCreateIfNotExistsScript()).AppendLine().AppendLine("GO");
    foreach (var entry in assembly.Migrations.OrderBy(x => x.Key, StringComparer.Ordinal).TakeWhile(x => string.CompareOrdinal(x.Key, item.Target) <= 0))
    {
        var migration = assembly.CreateMigration(entry.Value, item.Context.Database.ProviderName!);
        var targetModel = item.Context.GetService<IModelRuntimeInitializer>().Initialize(migration.TargetModel, designTime: true);
        var transaction = false;
        foreach (var command in sqlGenerator.Generate(migration.UpOperations, targetModel))
        {
            if (command.TransactionSuppressed && transaction) { script.AppendLine("COMMIT;").AppendLine("GO"); transaction = false; }
            if (!command.TransactionSuppressed && !transaction) { script.AppendLine("BEGIN TRANSACTION;").AppendLine("GO"); transaction = true; }
            script.AppendLine(command.CommandText).AppendLine("GO");
        }
        if (!transaction) script.AppendLine("BEGIN TRANSACTION;").AppendLine("GO");
        script.AppendLine(history.GetInsertScript(new HistoryRow(entry.Key, "10.0.12"))).AppendLine("GO");
        script.AppendLine("COMMIT;").AppendLine("GO");
    }
    var output = Path.Combine(args[0], item.File);
    using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    using var writer = new StreamWriter(stream);
    writer.Write(script.ToString());
    Console.WriteLine($"Generated {item.File} through {item.Target}; no database connection.");
}
