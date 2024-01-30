using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Mvc;
using pushNotification.service.cdp.Core.Config;

namespace pushNotification.service.cdp.Controllers
{
    /// <summary>
    /// For Test Pub Sub Usage
    /// </summary>

    [ApiController]
    [Route("api/pubsub")]
    public class PubSubTestController : ControllerBase
    {
        private readonly CloudConfig _cloudConfig; 
        private readonly ILogger<PubSubTestController> _logger;

        private SubscriberClient _subscriber;

        public PubSubTestController(CloudConfig pubSubConfig, ILogger<PubSubTestController> logger)
        {
            _cloudConfig = pubSubConfig;
            _logger = logger;
        }

        [HttpPost(nameof(PublishMessage))]
        public async Task<IActionResult> PublishMessage([FromBody] string message)
        {
            TopicName topicName = TopicName.FromProjectTopic(_cloudConfig.ProjectId, _cloudConfig.TopicId);
            PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
            // Conflic witch  Google.Cloud.PubSub.V1 
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            _logger.LogInformation("Message:" + message);
            string messageId = await publisher.PublishAsync(messageBytes);
            _logger.LogInformation("Publish message id:" + messageId);

            return Ok(new { MessageId = messageId });
        }

        [HttpPost(nameof(StartSubscriber))]
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await StartSubscriber();
        }

        [HttpPost("StopSubscriber")]
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _subscriber.StopAsync(CancellationToken.None);
            _logger.LogInformation("Subscriber stopped.");
        }


        protected async Task StartSubscriber()
        {
            SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(_cloudConfig.ProjectId, _cloudConfig.SubscriptionId);
            _subscriber = await SubscriberClient.CreateAsync(subscriptionName);

            await _subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
            {
                string text = System.Text.Encoding.UTF8.GetString(message.Data.ToArray());
                _logger.LogInformation($"Received message {message.MessageId}: {text}");
           
                // Process Revceive Message...

                
                return Task.FromResult(SubscriberClient.Reply.Ack);
            });
        }


    }
}
