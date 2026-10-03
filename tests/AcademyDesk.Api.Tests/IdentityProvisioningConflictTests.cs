using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class IdentityProvisioningConflictTests
{
    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void Only_the_target_unique_name_index_is_a_name_conflict(int number)
    {
        foreach (var index in new[] { "RoleNameIndex", "UserNameIndex" })
        {
            Assert.True(IdentityProvisioningConflict.MatchesSqlError(number, "Duplicate unique index '" + index + "'.", index));
            Assert.False(IdentityProvisioningConflict.MatchesSqlError(number, "Duplicate unique index 'Prefix" + index + "'.", index));
            Assert.False(IdentityProvisioningConflict.MatchesSqlError(number, "Duplicate unique index 'PK_AspNetUsers'.", index));
        }
    }

    [Fact]
    public void Deadlock_is_retryable_without_claiming_that_a_name_already_exists()
    {
        Assert.True(IdentityProvisioningConflict.MatchesSqlError(1205, "Deadlock victim; rerun the transaction.", "RoleNameIndex"));
        Assert.True(IdentityProvisioningConflict.MatchesSqlError(1205, "Deadlock victim; rerun the transaction.", "UserNameIndex"));
    }

    [Theory]
    [InlineData(51006)]
    [InlineData(3621)]
    [InlineData(-2)]
    [InlineData(1204)]
    [InlineData(547)]
    [InlineData(2602)]
    public void Other_SQL_failures_are_not_conflicts_even_when_the_message_mentions_the_index(int number)
    {
        Assert.False(IdentityProvisioningConflict.MatchesSqlError(number, "Failure mentioning 'RoleNameIndex'.", "RoleNameIndex"));
        Assert.False(IdentityProvisioningConflict.MatchesSqlError(number, "Failure mentioning 'UserNameIndex'.", "UserNameIndex"));
    }

    [Theory]
    [InlineData("RoleNameIndex", "UserNameIndex")]
    [InlineData("UserNameIndex", "RoleNameIndex")]
    [InlineData("EmailIndex", "UserNameIndex")]
    public void A_different_unique_constraint_is_not_the_requested_name_collision(string actual, string expected)
    {
        Assert.False(IdentityProvisioningConflict.MatchesSqlError(2601, "Duplicate unique index '" + actual + "'.", expected));
    }

    [Fact]
    public void Non_provider_errors_are_never_classified_from_message_text()
    {
        foreach (var exception in new Exception[] { new InvalidOperationException("1205 deadlock"), new DbUpdateException("2601 'RoleNameIndex' 'UserNameIndex'"), new OperationCanceledException() })
        {
            Assert.False(IdentityProvisioningConflict.RoleName(exception)); Assert.False(IdentityProvisioningConflict.UserName(exception));
        }
    }
}
