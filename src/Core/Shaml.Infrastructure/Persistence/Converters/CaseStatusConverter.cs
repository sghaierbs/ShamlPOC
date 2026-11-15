using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shaml.Domain.Cases;

namespace Shaml.Infrastructure.Persistence.Converters;

public sealed class CaseStatusConverter()
    : ValueConverter<CaseStatus, string>(
        status => ToDatabase(status),
        value => FromDatabase(value))
{
    private static string ToDatabase(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.PendingAssignment => "PENDING_ASSIGNMENT",
            CaseStatus.UnderStudy => "UNDER_STUDY",
            CaseStatus.UnderPreparation => "UNDER_PREPARATION",
            CaseStatus.UnderExecution => "UNDER_EXECUTION",
            CaseStatus.Completed => "COMPLETED",
            CaseStatus.Archived => "ARCHIVED",
            CaseStatus.Cancelled => "CANCELLED",
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
    }

    private static CaseStatus FromDatabase(string value)
    {
        return value switch
        {
            "PENDING_ASSIGNMENT" => CaseStatus.PendingAssignment,
            "UNDER_STUDY" => CaseStatus.UnderStudy,
            "UNDER_PREPARATION" => CaseStatus.UnderPreparation,
            "UNDER_EXECUTION" => CaseStatus.UnderExecution,
            "COMPLETED" => CaseStatus.Completed,
            "ARCHIVED" => CaseStatus.Archived,
            "CANCELLED" => CaseStatus.Cancelled,
            _ => throw new InvalidOperationException(
                $"Unknown case status: {value}")
        };
    }
}