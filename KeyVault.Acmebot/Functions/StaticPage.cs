using Azure.WebJobs.Extensions.HttpApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace KeyVault.Acmebot.Functions;

public class StaticPage : HttpFunctionBase
{
    public StaticPage(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    [Function($"{nameof(StaticPage)}_{nameof(Serve)}")]
    public IActionResult Serve(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*path}")] HttpRequest req,
        ILogger log)
    {
        if (!IsAuthenticationEnabled || !User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        return LocalStaticApp();
    }
}
