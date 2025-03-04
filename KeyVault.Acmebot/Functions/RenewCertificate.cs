﻿using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using DurableTask.TypedProxy;

using KeyVault.Acmebot.Internal;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace KeyVault.Acmebot.Functions;

public class RenewCertificate : HttpFunctionBase
{
    public RenewCertificate(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    [Microsoft.Azure.Functions.Worker.Function($"{nameof(RenewCertificate)}_{nameof(Orchestrator)}")]
    public async Task Orchestrator([Microsoft.Azure.Functions.Worker.OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var certificateName = context.GetInput<string>();

        var activity = context.CreateActivityProxy<ISharedActivity>();

        // 証明書の更新処理を開始
        var certificatePolicyItem = await activity.GetCertificatePolicy(certificateName);

        await context.CallSubOrchestratorAsync(nameof(SharedOrchestrator.IssueCertificate), certificatePolicyItem);
    }

    [Microsoft.Azure.Functions.Worker.Function($"{nameof(RenewCertificate)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [Microsoft.Azure.Functions.Worker.HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/certificate/{certificateName}/renew")] HttpRequest req,
        string certificateName,
        [Microsoft.Azure.Functions.Worker.DurableClient] IDurableClient starter,
        ILogger log)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        if (!User.HasIssueCertificateRole())
        {
            return Forbid();
        }

        // Function input comes from the request content.
        var instanceId = await starter.StartNewAsync($"{nameof(RenewCertificate)}_{nameof(Orchestrator)}", null, certificateName);

        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        return AcceptedAtFunction($"{nameof(GetInstanceState)}_{nameof(GetInstanceState.HttpStart)}", new { instanceId }, null);
    }
}
