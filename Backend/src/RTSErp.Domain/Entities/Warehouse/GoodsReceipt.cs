using RTSErp.Domain.Common;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Warehouse;

public class GoodsReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly ReceiptDate { get; set; }

    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    public Guid WarehouseId { get; set; }

    public string? PreparedBy { get; set; }
    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }

    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

    public ICollection<GoodsReceiptLine> Lines { get; set; } = [];
}
