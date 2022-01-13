namespace CosmosDbCloner
{
    public class AppSettings
    {
        public string? SrcEndpointUri { get; set; }
        public string? SrcPrimaryKey { get; set; }
        public string? SrcDatabaseId { get; set; }
        public string? SrcContainerId { get; set; }
        public string? TargetEndpointUri { get; set; }
        public string? TargetPrimaryKey { get; set; }
        public string? TargetDatabaseId { get; set; }
        public string? TargetContainerId { get; set; }

    }
}
