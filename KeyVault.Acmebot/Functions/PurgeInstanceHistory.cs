using System;
using System.Threading.Tasks;

using DurableTask.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace KeyVault.Acmebot.Functions;

public class PurgeInstanceHistory
{
    [Microsoft.Azure.Functions.Worker.Function($"{nameof(PurgeInstanceHistory)}_{nameof(Timer)}")]
    public Task Timer([Microsoft.Azure.Functions.Worker.TimerTrigger("0 0 0 1 * *")] TimerInfo timer, [Microsoft.Azure.Functions.Worker.DurableClient] IDurableClient starter)
    {
        return starter.PurgeInstanceHistoryAsync(
            DateTime.MinValue,
            DateTime.UtcNow.AddMonths(-1),
            new[]
            {
                OrchestrationStatus.Completed,
                OrchestrationStatus.Failed
            });
    }
}
