using System;
using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using DurableTask.TypedProxy;

using KeyVault.Acmebot.Internal;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace KeyVault.Acmebot.Functions;

public class RevokeCertificate : HttpFunctionBase
{
    public RevokeCertificate(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    [Function($"{nameof(RevokeCertificate)}_{nameof(Orchestrator)}")]
    public async Task Orchestrator([Microsoft.Azure.Functions.Worker.OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var certificateName = context.GetInput<string>();

        var activity = context.CreateActivityProxy<ISharedActivity>();

        await activity.RevokeCertificate(certificateName);
    }

    [Function($"{nameof(RevokeCertificate)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/certificate/{certificateName}/revoke")] HttpRequest req,
        string certificateName,
        [Microsoft.Azure.Functions.Worker.DurableClient] IDurableClient starter,
        ILogger log)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        if (!User.HasRevokeCertificateRole())
        {
            return Forbid();
        }

        // Function input comes from the request content.
        var instanceId = await starter.StartNewAsync($"{nameof(RevokeCertificate)}_{nameof(Orchestrator)}", null, certificateName);

        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId, TimeSpan.FromMinutes(1), returnInternalServerErrorOnFailure: true);
    }
}
