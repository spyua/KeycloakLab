using Google.Cloud.PubSub.V1;

namespace pushNotification.service.cdp.Service
{
    public class PubSubSubscriberService : BackgroundService
    {
        private readonly CloudConfig _cloudConfig;
        private readonly ILogger<PubSubSubscriberService> _logger;
        private SubscriberClient _subscriber;

        public PubSubSubscriberService(CloudConfig cloudConfig, ILogger<PubSubSubscriberService> logger)
        {
            _cloudConfig = cloudConfig;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Start Subscriber Service");
            SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(_cloudConfig.ProjectId, _cloudConfig.SubscriptionId);
            
            _subscriber = await SubscriberClient.CreateAsync(subscriptionName);
            
            _logger.LogInformation("Subscriber created for subscription " + _cloudConfig.SubscriptionId);

            
            await _subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
            {
                string text = System.Text.Encoding.UTF8.GetString(message.Data.ToArray());
                _logger.LogInformation($"Received message {message.MessageId}: {text}");

                // Process Revceive Message...

                return Task.FromResult(SubscriberClient.Reply.Ack);
            });

            // Wait for the subscriber to finish (shut down gracefully when cancelled).
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            finally
            {
                await _subscriber.StopAsync(CancellationToken.None);
                _logger.LogInformation("Subscriber stopped.");
            }
            
        }
    }

}

