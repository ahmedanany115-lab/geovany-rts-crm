namespace RTSErp.Domain.Entities.Audit;

public class AuditLog
{
    public Guid     Id         { get; set; } = Guid.NewGuid();
    public Guid?    UserId     { get; set; }
    public string   UserName   { get; set; } = string.Empty;
    public string   UserEmail  { get; set; } = string.Empty;
    public string   Action     { get; set; } = string.Empty;
    public string   Module     { get; set; } = string.Empty;
    public string?  EntityType { get; set; }
    public Guid?    EntityId   { get; set; }
    public string?  EntityName { get; set; }
    public string?  Reference  { get; set; }
    public string   Status     { get; set; } = "Success";
    public string?  Details    { get; set; }
    public string?  IpAddress  { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public static class AuditActions
{
    public const string Login       = "Login";
    public const string Logout      = "Logout";
    public const string LoginFailed = "Login Failed";
    public const string Created     = "Created";
    public const string Updated     = "Updated";
    public const string Deleted     = "Deleted";
    public const string Submitted   = "Submitted";
    public const string Approved    = "Approved";
    public const string Rejected    = "Rejected";
    public const string Cancelled   = "Cancelled";
    public const string Posted      = "Posted";
    public const string Reversed    = "Reversed";
    public const string Completed   = "Completed";
    public const string Uploaded    = "Uploaded";
    public const string Downloaded  = "Downloaded";
    public const string Converted   = "Converted";
}

public static class AuditModules
{
    public const string Auth        = "Auth";
    public const string CRM         = "CRM";
    public const string Sales       = "Sales";
    public const string Purchasing  = "Purchasing";
    public const string Inventory   = "Inventory";
    public const string Finance     = "Finance";
    public const string HR          = "HR";
    public const string Maintenance = "Maintenance";
    public const string Documents   = "Documents";
    public const string Users       = "Users";
}
