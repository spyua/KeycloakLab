namespace pushNotification.service.cdp.Core.Config
{
    public class KeycloakOptions
    {

        public string RootURL { get; set; }
        public string Realm { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string GrantType { get; set; }
        public bool SaveTokens { get; set; }

        // For Endpoint Setting
        public string Metadata { get; set; }
        public string TokenExchange { get; set; }
        public string POSTToken { get; set; }
        public string ServerRealmEndpoint  => $"{RootURL}/realms/{Realm}";
        public string POSTTokenEndpoint => $"{ServerRealmEndpoint}/{POSTToken}";
        public string TokenChangeEndpoint => $"{ServerRealmEndpoint}/{TokenExchange}";
        public string MetadataEndpoint => $"{ServerRealmEndpoint}/{Metadata}";
    }
}
