using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using KeyVault.Acmebot.Internal;
using KeyVault.Acmebot.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace KeyVault.Acmebot.Functions;

public class AddCertificate : HttpFunctionBase
{
    public AddCertificate(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    [Function($"{nameof(AddCertificate)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [Microsoft.Azure.Functions.Worker.DurableClient] IDurableClient starter, // Use the correct attribute for .NET Worker (Isolated Process)
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/certificate")] CertificatePolicyItem certificatePolicyItem,
        FunctionContext executionContext)
    {
        var log = executionContext.GetLogger("AddCertificate");

        if (!User.Identity.IsAuthenticated)
        {
            var response = executionContext.GetHttpResponseData();
            response.StatusCode = HttpStatusCode.Unauthorized;
            return response;
        }

        if (!User.HasIssueCertificateRole())
        {
            var response = executionContext.GetHttpResponseData();
            response.StatusCode = HttpStatusCode.Forbidden;
            return response;
        }

        if (!TryValidateModel(certificatePolicyItem))
        {
            var response = executionContext.GetHttpResponseData();
            response.StatusCode = HttpStatusCode.BadRequest;
            return response;
        }

        if (string.IsNullOrEmpty(certificatePolicyItem.CertificateName))
        {
            certificatePolicyItem.CertificateName = certificatePolicyItem.DnsNames[0].Replace("*", "wildcard").Replace(".", "-");
        }

        // Function input comes from the request content.
        var instanceId = await starter.StartNewAsync(nameof(SharedOrchestrator.IssueCertificate), certificatePolicyItem);

        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        var acceptedResponse = executionContext.GetHttpResponseData();
        acceptedResponse.StatusCode = HttpStatusCode.Accepted;
        acceptedResponse.Headers.Add("Location", $"{nameof(GetInstanceState)}_{nameof(GetInstanceState.HttpStart)}?instanceId={instanceId}");
        return acceptedResponse;
    }
}
