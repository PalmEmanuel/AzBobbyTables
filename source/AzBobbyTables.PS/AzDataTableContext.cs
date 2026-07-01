using System;

namespace PipeHow.AzBobbyTables;

public enum AzDataTableConnectionType
{
    ConnectionString,
    SAS,
    Key,
    Token,
    ManagedIdentity
}

public class AzDataTableContext
{
    public string TableName { get; set; }
    public AzDataTableConnectionType ConnectionType { get; }
    public int MaxConnectionsPerServer { get; }

    internal string ConnectionString { get; }
    internal Uri SharedAccessSignature { get; }
    internal string StorageAccountName { get; }
    internal string StorageAccountKey { get; }
    internal string ClientId { get; }
    internal string Token { get; }

    internal AzDataTableContext(string tableName, AzDataTableConnectionType connectionType, string connectionString, string storageAccountName, string storageAccountKey, Uri sharedAccessSignature, string clientId, string token, int maxConnectionsPerServer = 0)
    {
        TableName = tableName;
        ConnectionType = connectionType;
        ConnectionString = connectionString;
        SharedAccessSignature = sharedAccessSignature;
        StorageAccountName = storageAccountName;
        StorageAccountKey = storageAccountKey;
        Token = token;
        ClientId = clientId;
        MaxConnectionsPerServer = maxConnectionsPerServer;
    }
}
