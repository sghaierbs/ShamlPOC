using Elsa.Scheduling.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Shaml.Workflows.Definitions;

public sealed class ShamlCaseLifecycle : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Name = "ShamlCaseLifecycle";

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Shaml case lifecycle workflow started."),

                // Temporary suspension to verify that
                // workflow state persists in PostgreSQL.
                Delay.FromHours(24),

                new WriteLine("Shaml case lifecycle workflow resumed.")
            }
        };
    }
}