using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Warehouse;

public class GoodsReceiptLine : BaseEntity
{
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;

    public Guid? ProductId { get; set; }

    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? SerialNumber { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
