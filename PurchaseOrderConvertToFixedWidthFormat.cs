using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Medal.PurchaseOrder
{
    public class PurchaseOrderConvertToFixedWidthFormat
    {
        private readonly ILogger<PurchaseOrderConvertToFixedWidthFormat> _logger;

        public PurchaseOrderConvertToFixedWidthFormat(ILogger<PurchaseOrderConvertToFixedWidthFormat> logger)
        {
            _logger = logger;
        }

        [Function("PurchaseOrderConvertToFixedWidthFormat")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
