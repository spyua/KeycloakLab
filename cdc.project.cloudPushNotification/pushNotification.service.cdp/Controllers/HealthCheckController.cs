using Microsoft.AspNetCore.Mvc;

namespace pushNotification.service.cdp.Controllers
{

    [ApiController]
    [Route("/health")]
    public class HealthCheckController
    {
        private readonly ILogger<HealthCheckController> logger;

        public HealthCheckController(ILogger<HealthCheckController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        public string GetRoot()
        {
            logger.LogInformation("Health check");

            return "RESTful Service Work";
        }
    }


}
