namespace pushNotification.service.cdp.Core.Config
{
    public class CloudOption
    {
        /// <summary>
        /// Cloud Project ID
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Pub/Sub Topic ID
        /// </summary>
        public string TopicId { get; set; } = string.Empty;

        /// <summary>
        /// Subscription ID
        /// </summary>
        public string SubscriptionId { get; set; } = string.Empty;

    }
}
