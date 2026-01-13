using Azure.Data.Tables;

namespace QuiperTracker.Services;

public class LoginAuditService
{
    private readonly TableClient _tableClient;

    public LoginAuditService(IConfiguration configuration)
    {
        var connectionString = configuration["Storage:ConnectionString"]
            ?? throw new InvalidOperationException("Storage:ConnectionString is missing.");

        // Table name provided by user is "Loginaudit".
        _tableClient = new TableClient(connectionString, "Loginaudit");
        _tableClient.CreateIfNotExists();
    }

    public Task LogAsync(string userId, string email, string status)
    {
        var entity = new TableEntity(
            partitionKey: DateTime.UtcNow.ToString("yyyy-MM-dd"),
            rowKey: Guid.NewGuid().ToString())
        {
            { "UserId", userId },
            { "Email", email ?? string.Empty },
            { "Status", status },
            { "LoginTime", DateTime.UtcNow }
        };

        return _tableClient.AddEntityAsync(entity);
    }
}
