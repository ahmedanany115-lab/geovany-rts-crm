namespace RTSErp.Shared.Constants;

public static class AppClaimTypes
{
    /// <summary>Custom JWT claim type carrying a single permission code (e.g. "crm.customers.write"). One claim per permission.</summary>
    public const string Permission = "permission";
}
