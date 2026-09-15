using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class SearchController : BaseApiController
{
    private readonly IApplicationDbContext _db;
    public SearchController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { results = Array.Empty<object>() });

        var term = q.Trim().ToLower();
        var results = new List<object>();

        // Customers
        var customers = await _db.BusinessPartners
            .Where(b => !b.IsDeleted && b.PartnerType == Domain.Entities.Accounting.BusinessPartnerType.Customer
                     && (b.Name.ToLower().Contains(term) || (b.Code != null && b.Code.ToLower().Contains(term))
                         || (b.Phone != null && b.Phone.Contains(term))))
            .Take(5)
            .Select(b => new { b.Id, b.Name, b.Code, type = "Customer", href = "/crm/customers" })
            .ToListAsync(ct);
        results.AddRange(customers);

        // Suppliers
        var suppliers = await _db.BusinessPartners
            .Where(b => !b.IsDeleted && b.PartnerType == Domain.Entities.Accounting.BusinessPartnerType.Supplier
                     && (b.Name.ToLower().Contains(term) || (b.Code != null && b.Code.ToLower().Contains(term))))
            .Take(3)
            .Select(b => new { b.Id, b.Name, b.Code, type = "Supplier", href = "/erp/suppliers" })
            .ToListAsync(ct);
        results.AddRange(suppliers);

        // Sales Orders
        var salesOrders = await _db.SalesOrders
            .Include(o => o.Customer)
            .Where(o => !o.IsDeleted
                     && (o.SONumber.ToLower().Contains(term) || o.Customer.Name.ToLower().Contains(term)))
            .Take(5)
            .Select(o => new { o.Id, Name = o.SONumber, Code = o.Customer.Name, type = "Sales Order", href = "/erp/sales-orders" })
            .ToListAsync(ct);
        results.AddRange(salesOrders);

        // Customer Invoices
        var invoices = await _db.CustomerInvoices
            .Include(i => i.Customer)
            .Where(i => !i.IsDeleted
                     && (i.InvoiceNumber.ToLower().Contains(term) || i.Customer.Name.ToLower().Contains(term)))
            .Take(5)
            .Select(i => new { i.Id, Name = i.InvoiceNumber, Code = i.Customer.Name, type = "Invoice", href = "/erp/customer-invoices" })
            .ToListAsync(ct);
        results.AddRange(invoices);

        // Products
        var products = await _db.Products
            .Where(p => !p.IsDeleted
                     && (p.Name.ToLower().Contains(term) || p.SKU.ToLower().Contains(term)))
            .Take(5)
            .Select(p => new { p.Id, p.Name, Code = p.SKU, type = "Product", href = "/erp/products" })
            .ToListAsync(ct);
        results.AddRange(products);

        // Maintenance Contracts
        var contracts = await _db.MaintenanceContracts
            .Include(c => c.Customer)
            .Where(c => !c.IsDeleted
                     && (c.ContractNumber.ToLower().Contains(term) || c.Customer.Name.ToLower().Contains(term)))
            .Take(3)
            .Select(c => new { c.Id, Name = c.ContractNumber, Code = c.Customer.Name, type = "Contract", href = "/erp/maintenance/contracts" })
            .ToListAsync(ct);
        results.AddRange(contracts);

        // Purchase Orders
        var pos = await _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Where(o => !o.IsDeleted
                     && (o.PONumber.ToLower().Contains(term) || o.Supplier.Name.ToLower().Contains(term)))
            .Take(3)
            .Select(o => new { o.Id, Name = o.PONumber, Code = o.Supplier.Name, type = "Purchase Order", href = "/erp/purchase-orders" })
            .ToListAsync(ct);
        results.AddRange(pos);

        // Company documents
        var docs = await _db.CompanyDocuments
            .Where(d => !d.IsDeleted
                     && (d.Name.ToLower().Contains(term) || d.Category.ToLower().Contains(term)))
            .Take(3)
            .Select(d => new { d.Id, d.Name, Code = d.Category, type = "Document", href = "/documents" })
            .ToListAsync(ct);
        results.AddRange(docs);

        return Ok(new { results });
    }
}
