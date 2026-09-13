using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Operational;

namespace RTSErp.Domain.Entities.Maintenance;

/// <summary>
/// Equipment / asset item covered by a MaintenanceContract.
/// </summary>
public class ContractEquipment : BaseEntity
{
    public Guid   ContractId    { get; set; }
    public MaintenanceContract Contract { get; set; } = null!;

    public string ItemName     { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Brand        { get; set; }
    public string? Model        { get; set; }
    public string? Location     { get; set; }    // room / floor / site
    public DateOnly? InstallDate { get; set; }
    public string? Notes        { get; set; }

    // Optional link to a Product in the catalogue
    public Guid?    ProductId   { get; set; }
    public Product? Product     { get; set; }
}
