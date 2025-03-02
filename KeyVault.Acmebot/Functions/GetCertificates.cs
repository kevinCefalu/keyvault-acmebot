using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using DurableTask.TypedProxy;

using KeyVault.Acmebot.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace KeyVault.Acmebot.Functions;

public class GetCertificates : HttpFunctionBase
{
    public GetCertificates(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    [Function($"{nameof(GetCertificates)}_{nameof(Orchestrator)}")]
    public Task<IReadOnlyList<CertificateItem>> Orchestrator([Microsoft.Azure.Functions.Worker.OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var activity = context.CreateActivityProxy<ISharedActivity>();

        return activity.GetAllCertificates();
    }

    [Function($"{nameof(GetCertificates)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/certificates")] HttpRequest req,
        [Microsoft.Azure.Functions.Worker.DurableClient] IDurableClient starter,
        ILogger log)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        // Function input comes from the request content.
        var instanceId = await starter.StartNewAsync($"{nameof(GetCertificates)}_{nameof(Orchestrator)}");

        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId, TimeSpan.FromMinutes(1), returnInternalServerErrorOnFailure: true);
    }
}
