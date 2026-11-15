
using System.Data;
using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Domain.Cases;

namespace Shaml.Infrastructure.Persistence;

public sealed class CaseAssignmentStore(
    ShamlDbContext db) : ICaseAssignmentStore
{
    // Replace these values with the actual codes in Roles.
    private const string SpecialistRoleCode = "SPECIALIST";
    private const string DirectorRoleCode = "CENTER_DIRECTOR";

    public async Task<CaseAssignmentResult> AssignAutomaticallyAsync(
        Guid caseId,
        CancellationToken ct = default)
    {
        // First lookup is only to discover the center.
        var centerId = await db.Cases
            .AsNoTracking()
            .Where(x => x.Id == caseId)
            .Select(x => (long?)x.CenterId)
            .SingleOrDefaultAsync(ct);

        if (centerId is null)
            throw new InvalidOperationException(
                $"Case {caseId} was not found.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, ct);

        // Lock the center first. This serializes assignment
        // decisions and workload calculations within the center.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM shaml."Centers"
             WHERE "Id" = {centerId.Value}
             FOR UPDATE
             """, ct);

        // Lock the case and re-read it inside the transaction.
        var caseRecord = await db.Cases
            .FromSqlInterpolated(
                $"""
                 SELECT *
                 FROM shaml."Cases"
                 WHERE "Id" = {caseId}
                 FOR UPDATE
                 """)
            .SingleAsync(ct);

        // Another worker may have already assigned it.
        var currentAssignment = await db.CaseAssignments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId &&
                        x.EndedAtUtc == null)
            .Select(x => (long?)x.UserRoleId)
            .SingleOrDefaultAsync(ct);

        if (currentAssignment.HasValue)
        {
            await transaction.CommitAsync(ct);

            return new CaseAssignmentResult(
                caseId,
                currentAssignment.Value,
                "ALREADY_ASSIGNED");
        }

        if (caseRecord.Status is not (
            CaseStatus.UnderStudy or
            CaseStatus.PendingAssignment))
        {
            return new CaseAssignmentResult(
                caseId,
                null,
                "NOT_ELIGIBLE");
        }

        long? selectedUserRoleId = null;

        if (caseRecord.Status == CaseStatus.UnderStudy)
        {
            selectedUserRoleId = await FindLeastLoadedSpecialistAsync(
                centerId.Value, ct);
        }

        // Waiting cases and cases without specialists
        // are assigned to the Center Director.
        selectedUserRoleId ??= await FindCenterDirectorAsync(
            centerId.Value, ct);

        if (selectedUserRoleId is null)
        {
            return new CaseAssignmentResult(
                caseId,
                null,
                "NO_ELIGIBLE_ASSIGNEE");
        }

        var assignment = new CaseAssignment(
            caseId,
            selectedUserRoleId.Value);

        db.CaseAssignments.Add(assignment);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new CaseAssignmentResult(
            caseId,
            selectedUserRoleId.Value,
            "ASSIGNED");
    }

    private async Task<long?> FindCenterDirectorAsync(
        long centerId,
        CancellationToken ct)
    {
        return await (
            from ur in db.UserRoles
            join role in db.Roles on ur.RoleId equals role.Id
            join user in db.Users
                on ur.UserSocialId equals user.Id
            where ur.CenterId == centerId
                  && ur.IsActive
                  && role.IsActive
                  && user.IsActive
                  && role.Code == DirectorRoleCode
            orderby ur.Id
            select (long?)ur.Id
        ).FirstOrDefaultAsync(ct);
    }

    private async Task<long?> FindLeastLoadedSpecialistAsync(
        long centerId,
        CancellationToken ct)
    {
        var eligibleSpecialists =
            from ur in db.UserRoles
            join role in db.Roles on ur.RoleId equals role.Id
            join user in db.Users
                on ur.UserSocialId equals user.Id
            where ur.CenterId == centerId
                  && ur.IsActive
                  && role.IsActive
                  && user.IsActive
                  && role.Code == SpecialistRoleCode
            select ur.Id;

        var activeAssignments =
            from assignment in db.CaseAssignments
            join caseRecord in db.Cases
                on assignment.CaseId equals caseRecord.Id
            where assignment.EndedAtUtc == null
                  && caseRecord.CenterId == centerId
                  && (
                      caseRecord.Status == CaseStatus.UnderStudy ||
                      caseRecord.Status == CaseStatus.UnderPreparation ||
                      caseRecord.Status == CaseStatus.UnderExecution
                  )
            select assignment;

        return await (
            from specialistId in eligibleSpecialists
            join assignment in activeAssignments
                on specialistId equals assignment.UserRoleId
                into assignments
            orderby assignments.Count(), specialistId
            select (long?)specialistId
        ).FirstOrDefaultAsync(ct);
    }
}
