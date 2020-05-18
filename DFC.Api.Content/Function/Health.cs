using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Health
    {
        [FunctionName("HealthCheck")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Health/HealthCheck")] HttpRequest req)
        {
            return new OkObjectResult("Ok");
        }
    }
}
