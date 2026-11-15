
using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Cases.CreateCase;
using Shaml.Domain.Cases;
using Shaml.Infrastructure.Persistence.Messaging;
using Shaml.Infrastructure.Persistence.Workflows;

namespace Shaml.Infrastructure.Persistence;

public sealed class CaseIntakeStore(
    ShamlDbContext db,
    OutboxWriter outbox) : ICaseIntakeStore
{
    public async Task<CreateCaseResponse> CreateAsync(
        CreateCaseRequest request,
        CancellationToken ct = default)
    {
        var source = request.SourceSystem.Trim();
        var externalId = request.ExternalCaseId.Trim();

        if (source.Length is 0 or > 100 ||
            externalId.Length is 0 or > 100 ||
            request.CenterId <= 0)
        {
            throw new ArgumentException(
                "Invalid case intake request.");
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, ct);

        // Serialize capacity decisions for this center.
        var connection =
            (NpgsqlConnection)db.Database.GetDbConnection();

        var pgTransaction =
            (NpgsqlTransaction)transaction.GetDbTransaction();

        await using (var command = new NpgsqlCommand(
            """
            SELECT "Id"
            FROM shaml."Centers"
            WHERE "Id" = @centerId
            FOR UPDATE
            """,
            connection,
            pgTransaction))
        {
            command.Parameters.AddWithValue(
                "centerId", request.CenterId);

            var centerId = await command.ExecuteScalarAsync(ct);

            if (centerId is null)
                throw new CaseIntakeException(
                    "CENTER_NOT_FOUND",
                    "The selected center does not exist.");
        }

        // The duplicate check must include ALL case statuses.
        var duplicate = await db.Cases
            .AsNoTracking()
            .AnyAsync(x =>
                x.SourceSystem == source &&
                x.ExternalCaseId == externalId, ct);

        if (duplicate)
            throw new CaseIntakeException(
                "CASE_ALREADY_EXISTS",
                "The external case already exists.");

        var center = await db.Centers
            .AsNoTracking()
            .SingleAsync(x => x.Id == request.CenterId, ct);

        if (!center.IsActive || !center.IsReceivingCases)
            throw new CaseIntakeException(
                "CENTER_NOT_RECEIVING_CASES",
                "The center is not accepting cases.");

        // All three statuses consume operational capacity.
        var activeCount = await db.Cases.CountAsync(
            x => x.CenterId == request.CenterId &&
                (x.Status == CaseStatus.UnderStudy ||
                 x.Status == CaseStatus.UnderPreparation ||
                 x.Status == CaseStatus.UnderExecution),
            ct);

        var waitingCount = await db.Cases.CountAsync(
            x => x.CenterId == request.CenterId &&
                 x.Status == CaseStatus.PendingAssignment,
            ct);

        CaseStatus initialStatus;

        if (activeCount < center.OperationalCapacity)
        {
            initialStatus = CaseStatus.UnderStudy;
        }
        else if (waitingCount < center.WaitingListLimit)
        {
            initialStatus = CaseStatus.PendingAssignment;
        }
        else
        {
            throw new CaseIntakeException(
                "CENTER_CAPACITY_EXCEEDED",
                "The center's operational capacity and waiting list are full.");
        }

        // Create the case.
        var newCase = new Case(
            externalId,
            source,
            request.CenterId,
            initialStatus);

        db.Cases.Add(newCase);
        
        
        // Select the initial assignee while holding
        // the center's database row lock.
        var selectedUserRoleId = await SelectInitialAssigneeAsync(
            request.CenterId,
            initialStatus,
            ct);

        // Create the initial primary assignment.
        db.CaseAssignments.Add(
            new CaseAssignment(
                newCase.Id,
                selectedUserRoleId));


        // Register the workflow startup request.
        // Elsa will be started asynchronously after this
        // transaction commits through the outbox consumer.
        db.CaseWorkflowInstances.Add(
            new CaseWorkflowInstance
            {
                CaseId = newCase.Id,
                WorkflowDefinitionId = "ShamlCaseLifecycle",
                Status = "PENDING"
            });

        var status = initialStatus == CaseStatus.UnderStudy
            ? "UNDER_STUDY"
            : "PENDING_ASSIGNMENT";

        var payload = JsonSerializer.Serialize(new
        {
            CaseId = newCase.Id,
            newCase.SourceSystem,
            newCase.ExternalCaseId,
            newCase.CenterId,
            Status = status,
            newCase.CreatedAtUtc
        });

        // Persist the CaseCreated event in the same transaction.
        await outbox.AddAsync(
            "CaseCreated",
            payload,
            ct);

        try
        {
            // Saves Case + workflow registry + outbox event.
            await db.SaveChangesAsync(ct);

            // All records become visible atomically.
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName:
                    "UX_Cases_SourceSystem_ExternalCaseId"
            })
        {
            throw new CaseIntakeException(
                "CASE_ALREADY_EXISTS",
                "The external case already exists.");
        }

        return new CreateCaseResponse(
            newCase.Id,
            source,
            externalId,
            request.CenterId,
            status,
            newCase.CreatedAtUtc);
    }
    
    
private async Task<long> SelectInitialAssigneeAsync(
    long centerId,
    CaseStatus initialStatus,
    CancellationToken ct)
{
    // UNDER_STUDY:
    // Select the active Specialist with the fewest
    // currently active cases at this center.
    if (initialStatus == CaseStatus.UnderStudy)
    {
        var specialistId = await (
            from userRole in db.UserRoles
            join role in db.Roles
                on userRole.RoleId equals role.Id
            join user in db.Users
                on userRole.UserSocialId equals user.Id
            where userRole.CenterId == centerId
                  && userRole.IsActive
                  && role.IsActive
                  && role.Code == "SPECIALIST"
                  && user.IsActive
            select new
            {
                userRole.Id,

                ActiveCaseCount = db.CaseAssignments
                    .Count(a =>
                        a.UserRoleId == userRole.Id &&
                        a.EndedAtUtc == null &&
                        db.Cases.Any(c =>
                            c.Id == a.CaseId &&
                            c.CenterId == centerId &&
                            (c.Status == CaseStatus.UnderStudy ||
                             c.Status == CaseStatus.UnderPreparation ||
                             c.Status == CaseStatus.UnderExecution)))
            })
            .OrderBy(x => x.ActiveCaseCount)
            .ThenBy(x => x.Id)
            .Select(x => (long?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (specialistId.HasValue)
            return specialistId.Value;
    }

    // PENDING_ASSIGNMENT or no eligible Specialist:
    // Assign to the active Center Director.
    var directorId = await (
        from userRole in db.UserRoles
        join role in db.Roles
            on userRole.RoleId equals role.Id
        join user in db.Users
            on userRole.UserSocialId equals user.Id
        where userRole.CenterId == centerId
              && userRole.IsActive
              && role.IsActive
              && role.Code == "CENTER_DIRECTOR"
              && user.IsActive
        orderby userRole.Id
        select (long?)userRole.Id
    ).FirstOrDefaultAsync(ct);

    if (!directorId.HasValue)
    {
        throw new CaseIntakeException(
            "CENTER_ASSIGNEE_NOT_FOUND",
            "No eligible Specialist or Center Director " +
            "is available for this case.");
    }

    return directorId.Value;
}

}
