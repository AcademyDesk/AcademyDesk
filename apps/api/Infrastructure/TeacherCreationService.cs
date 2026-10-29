using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Infrastructure;

public sealed record TeacherCreationOutcome(int HttpStatus, TeacherSummary? Result = null, string? Error = null);

/// <summary>
/// Shared existing teacher-create operation. The caller owns approval, request
/// replay/duplicates, shared SQL/Identity transaction and mandatory audit/commit.
/// A successful service result is not a committed conversation receipt.
/// Never accepts a model-supplied role, tenant authority or account credentials.
/// </summary>
public sealed class TeacherCreationService(AcademyDeskDbContext db, IdentityDbContext identity,
    UserManager<ApplicationUser> users)
{
    public async Task<TeacherCreationOutcome> CreateAsync(ClaimsPrincipal principal, Guid academyId,
        CreateTeacherRequest request, CancellationToken token)
    {
        if ((db.Database.IsRelational() || identity.Database.IsRelational()) &&
            (!db.Database.IsSqlServer() || !identity.Database.IsSqlServer() ||
             db.Database.CurrentTransaction is not { } domainTransaction ||
             identity.Database.CurrentTransaction is not { } identityTransaction ||
             !ReferenceEquals(domainTransaction.GetDbTransaction(), identityTransaction.GetDbTransaction())))
            return new(503, Error: "Teacher creation requires an atomic host transaction.");
        if (principal.Identity?.IsAuthenticated != true || academyId == Guid.Empty ||
            !Guid.TryParse(users.GetUserId(principal), out var actorId)) return new(403);
        // Current database identity/role membership, never principal role claims.
        // The shared transaction holds this authorization through the host commit.
        if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academyId}", token);
        var actor = db.Database.IsSqlServer()
            ? await identity.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={actorId}")
                .AsNoTracking().SingleOrDefaultAsync(token)
            : await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId, token);
        if (actor is null || !actor.IsActive || !actor.IsPlatformOwner && actor.AcademyId != academyId) return new(403);
        if (!actor.IsPlatformOwner) {
            var memberships = db.Database.IsSqlServer()
                ? await identity.UserRoles.FromSqlInterpolated($"SELECT * FROM [AspNetUserRoles] WITH (UPDLOCK,HOLDLOCK) WHERE [UserId]={actorId}")
                    .AsNoTracking().ToArrayAsync(token)
                : await identity.UserRoles.AsNoTracking().Where(x => x.UserId == actorId).ToArrayAsync(token);
            var ids = memberships.Select(x => x.RoleId).ToArray();
            var roleQuery = db.Database.IsSqlServer()
                ? identity.Roles.FromSqlInterpolated($"SELECT * FROM [AspNetRoles] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] IN (SELECT [RoleId] FROM [AspNetUserRoles] WHERE [UserId]={actorId})")
                : identity.Roles.Where(x => ids.Contains(x.Id));
            if (!await roleQuery.AsNoTracking().AnyAsync(x => x.Name == "Owner" || x.Name == "AcademyAdmin", token)) return new(403);
        }
        var academy = await db.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academyId, token);
        if (academy is null) return new(404);
        if (!actor.IsPlatformOwner && (!academy.IsActive || !SubscriptionPlanCatalog.Allows(academy.EnabledModulesJson,
                SubscriptionPlanCatalog.ModuleForController(nameof(TeachersController))))) return new(403);
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return new(400, Error: "First name and last name are required.");
        if (request.BranchId.HasValue && !await db.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, token))
            return new(400, Error: "The selected branch does not belong to this academy.");

        // Preserve the Manual request's existing null/blank and metadata behavior.
        // Conversational callers must first apply their narrower field contract.
        var row = new Teacher { AcademyId = academyId, BranchId = request.BranchId,
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(), Specialties = request.Specialties?.Trim(), Qualifications = request.Qualifications?.Trim(),
            EmploymentType = request.EmploymentType?.Trim(), DateOfBirth = request.DateOfBirth, JoiningDate = request.JoiningDate,
            AddressLine1 = request.AddressLine1?.Trim(), City = request.City?.Trim(), State = request.State?.Trim(),
            PostalCode = request.PostalCode?.Trim(), CertificationsJson = request.CertificationsJson,
            AvailabilityJson = request.AvailabilityJson, CompensationJson = request.CompensationJson };
        db.Teachers.Add(row);
        await db.SaveChangesAsync(token);
        return new(201, new TeacherSummary(row.Id, row.FirstName, row.LastName, row.Email, row.Phone,
            row.Specialties, row.BranchId, row.IsActive));
    }
}
