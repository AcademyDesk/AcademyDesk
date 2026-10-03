using AcademyDesk.Api.Data;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class AcademyIdentityTransactionTests
{
    private const string Connection = "Server=127.0.0.1,49999;Database=Synthetic;User ID=synthetic;Password=Synthetic!Only;Encrypt=True;TrustServerCertificate=True";

    [Fact]
    public void Matching_configured_sql_contexts_are_accepted_without_opening_SQL()
    {
        using var academy = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(Connection).Options);
        using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(Connection).Options);
        Assert.Equal(Connection, AcademyIdentityTransaction.Validate(academy, identity));
        Assert.Null(academy.Database.CurrentTransaction); Assert.Null(identity.Database.CurrentTransaction);
        Assert.Equal(System.Data.ConnectionState.Closed, identity.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData("Database=Synthetic", "Database=Different")]
    [InlineData("127.0.0.1,49999", "127.0.0.1,49998")]
    [InlineData("User ID=synthetic", "User ID=different")]
    [InlineData("Synthetic!Only", "Different!Only")]
    public void Differing_store_or_principal_is_rejected_before_business_writes(string original, string changed)
    {
        using var academy = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(Connection).Options);
        using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(Connection.Replace(original, changed)).Options);
        var error = Assert.Throws<InvalidOperationException>(() => AcademyIdentityTransaction.Validate(academy, identity));
        Assert.DoesNotContain("Password", error.Message); Assert.DoesNotContain("Synthetic!Only", error.Message);
    }

    [Fact]
    public void Externally_supplied_connection_without_configured_string_is_rejected()
    {
        using var academy = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(Connection).Options);
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(Connection);
        using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(connection).Options);
        Assert.Throws<InvalidOperationException>(() => AcademyIdentityTransaction.Validate(academy, identity));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Non_SQL_store_on_either_side_is_rejected(bool academyIsSql)
    {
        var academyOptions = new DbContextOptionsBuilder<AcademyDeskDbContext>();
        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>();
        if (academyIsSql) { academyOptions.UseSqlServer(Connection); identityOptions.UseInMemoryDatabase("synthetic-identity"); }
        else { academyOptions.UseInMemoryDatabase("synthetic-academy"); identityOptions.UseSqlServer(Connection); }
        using var academy = new AcademyDeskDbContext(academyOptions.Options);
        using var identity = new IdentityDbContext(identityOptions.Options);
        Assert.Throws<InvalidOperationException>(() => AcademyIdentityTransaction.Validate(academy, identity));
    }
}
