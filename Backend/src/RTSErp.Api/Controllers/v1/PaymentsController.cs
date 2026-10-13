using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Operational.Payments;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/customerpayments")]
public class CustomerPaymentsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId, [FromQuery] PaymentStatus? status)
        => Ok(await Mediator.Send(new GetCustomerPaymentsQuery { CustomerId = customerId, Status = status }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerPaymentCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAccountingService accounting,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var payment = await db.CustomerPayments
            .Include(p => p.Invoices)
            .Include(p => p.BankAccount)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (payment is null) return NotFound();
        if (payment.Status == PaymentStatus.Cancelled) return NoContent();

        if (payment.Invoices.Any())
            return BadRequest(new { message = "This payment is allocated to invoices. Remove/reverse the invoice allocation before cancelling it." });

        if (payment.ChequeId.HasValue)
            return BadRequest(new { message = "This payment is linked to a cheque. Cancel the cheque from the Cheques screen instead." });

        if (payment.JournalEntryId.HasValue)
        {
            var reversed = await accounting.ReverseJournalEntryAsync(
                payment.JournalEntryId.Value,
                $"Cancelled customer payment {payment.PaymentNumber}",
                DateOnly.FromDateTime(DateTime.UtcNow),
                ct);
            if (!reversed.Succeeded)
                return BadRequest(new { message = string.Join(", ", reversed.Errors) });
        }

        if (payment.BankAccount is not null)
        {
            payment.BankAccount.CurrentBalance -= payment.Amount;
            payment.BankAccount.ModifiedAt = DateTime.UtcNow;
            payment.BankAccount.ModifiedBy = user.UserId;
        }

        payment.Status = PaymentStatus.Cancelled;
        payment.IsDeleted = true;
        payment.ModifiedAt = DateTime.UtcNow;
        payment.ModifiedBy = user.UserId;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/supplierpayments")]
public class SupplierPaymentsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetSupplierPaymentsQuery { SupplierId = supplierId }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierPaymentCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAccountingService accounting,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var payment = await db.SupplierPayments
            .Include(p => p.Invoices)
            .Include(p => p.BankAccount)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (payment is null) return NotFound();
        if (payment.Status == PaymentStatus.Cancelled) return NoContent();

        if (payment.Invoices.Any())
            return BadRequest(new { message = "This payment is allocated to supplier invoices. Remove/reverse the allocation before cancelling it." });

        if (payment.JournalEntryId.HasValue)
        {
            var reversed = await accounting.ReverseJournalEntryAsync(
                payment.JournalEntryId.Value,
                $"Cancelled supplier payment {payment.PaymentNumber}",
                DateOnly.FromDateTime(DateTime.UtcNow),
                ct);
            if (!reversed.Succeeded)
                return BadRequest(new { message = string.Join(", ", reversed.Errors) });
        }

        if (payment.BankAccount is not null)
        {
            payment.BankAccount.CurrentBalance += payment.Amount;
            payment.BankAccount.ModifiedAt = DateTime.UtcNow;
            payment.BankAccount.ModifiedBy = user.UserId;
        }

        payment.Status = PaymentStatus.Cancelled;
        payment.IsDeleted = true;
        payment.ModifiedAt = DateTime.UtcNow;
        payment.ModifiedBy = user.UserId;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/cheques")]
public class ChequesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? direction,
        [FromQuery] ChequeStatus? status,
        [FromQuery] Guid? customerId,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var q = db.Cheques
            .Include(c => c.Customer)
            .Include(c => c.Supplier)
            .Include(c => c.Currency)
            .Where(c => !c.IsDeleted);

        if (direction.HasValue) q = q.Where(c => (int)c.Direction == direction.Value);
        if (status.HasValue)    q = q.Where(c => c.Status == status.Value);
        if (customerId.HasValue) q = q.Where(c => c.CustomerId == customerId.Value);

        var items = await q.OrderByDescending(c => c.DueDate)
            .Select(c => new
            {
                c.Id, c.ChequeNumber, c.Direction,
                customerName = c.Customer != null ? c.Customer.Name : null,
                supplierName = c.Supplier != null ? c.Supplier.Name : null,
                c.BankName, c.Amount, c.AmountBase,
                currencyCode = c.Currency.Code,
                c.IssueDate, c.DueDate, c.ReceivedDate, c.Status,
                statusName   = c.Status.ToString(),
                c.Notes, c.CreatedAt,
            }).ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Receive(ReceiveChequeCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpPost("{id:guid}/deposit")]
    public async Task<IActionResult> Deposit(Guid id, DepositChequeCommand cmd)
    { cmd.Id = id; await Mediator.Send(cmd); return NoContent(); }

    [HttpPost("{id:guid}/bounce")]
    public async Task<IActionResult> Bounce(Guid id, [FromBody] DateOnly bounceDate)
    { await Mediator.Send(new BounceChequeCommand { Id = id, BounceDate = bounceDate }); return NoContent(); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAccountingService accounting,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var cheque = await db.Cheques
            .Include(c => c.BankAccount)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
        if (cheque is null) return NotFound();

        if (cheque.Status == ChequeStatus.Cleared)
            return BadRequest(new { message = "A cleared cheque cannot be deleted. Reverse the clearing transaction first." });

        var reversalDate = DateOnly.FromDateTime(DateTime.UtcNow);

        if (cheque.Status == ChequeStatus.Deposited && cheque.DepositJournalEntryId.HasValue)
        {
            var reversedDeposit = await accounting.ReverseJournalEntryAsync(
                cheque.DepositJournalEntryId.Value,
                $"Cancelled cheque deposit {cheque.ChequeNumber}",
                reversalDate,
                ct);
            if (!reversedDeposit.Succeeded)
                return BadRequest(new { message = string.Join(", ", reversedDeposit.Errors) });

            if (cheque.BankAccount is not null)
            {
                cheque.BankAccount.CurrentBalance -= cheque.Amount;
                cheque.BankAccount.ModifiedAt = DateTime.UtcNow;
                cheque.BankAccount.ModifiedBy = user.UserId;
            }
        }

        if (cheque.ReceiptJournalEntryId.HasValue)
        {
            var reversedReceipt = await accounting.ReverseJournalEntryAsync(
                cheque.ReceiptJournalEntryId.Value,
                $"Cancelled cheque {cheque.ChequeNumber}",
                reversalDate,
                ct);
            if (!reversedReceipt.Succeeded && cheque.Status != ChequeStatus.Bounced)
                return BadRequest(new { message = string.Join(", ", reversedReceipt.Errors) });
        }

        cheque.Status = ChequeStatus.Cancelled;
        cheque.IsDeleted = true;
        cheque.ModifiedAt = DateTime.UtcNow;
        cheque.ModifiedBy = user.UserId;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/banktransactions")]
public class BankTransactionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? bankAccountId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
        => Ok(await Mediator.Send(new GetBankTransactionsQuery { BankAccountId = bankAccountId, FromDate = fromDate, ToDate = toDate }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateBankTransactionCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Reverse(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAccountingService accounting,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var txn = await db.BankTransactions
            .Include(t => t.BankAccount)
            .Include(t => t.DestinationBankAccount)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
        if (txn is null) return NotFound();

        if (txn.JournalEntryId.HasValue)
        {
            var reversed = await accounting.ReverseJournalEntryAsync(
                txn.JournalEntryId.Value,
                $"Reversed bank transaction {txn.TransactionNumber}",
                DateOnly.FromDateTime(DateTime.UtcNow),
                ct);
            if (!reversed.Succeeded)
                return BadRequest(new { message = string.Join(", ", reversed.Errors) });
        }

        switch (txn.TransactionType)
        {
            case BankTransactionType.Deposit:
                txn.BankAccount.CurrentBalance -= txn.Amount;
                break;
            case BankTransactionType.Withdrawal:
                txn.BankAccount.CurrentBalance += txn.Amount;
                break;
            case BankTransactionType.Transfer:
                txn.BankAccount.CurrentBalance += txn.Amount;
                if (txn.DestinationBankAccount is not null)
                {
                    txn.DestinationBankAccount.CurrentBalance -= txn.Amount;
                    txn.DestinationBankAccount.ModifiedAt = DateTime.UtcNow;
                    txn.DestinationBankAccount.ModifiedBy = user.UserId;
                }
                break;
            default:
                return BadRequest(new { message = "This transaction type is managed by its source document and must be reversed there." });
        }

        txn.BankAccount.ModifiedAt = DateTime.UtcNow;
        txn.BankAccount.ModifiedBy = user.UserId;
        txn.IsDeleted = true;
        txn.ModifiedAt = DateTime.UtcNow;
        txn.ModifiedBy = user.UserId;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/bankaccounts")]
public class BankAccountsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive)
        => Ok(await Mediator.Send(new GetBankAccountsQuery { IsActive = isActive }));

    [HttpPost]
    public async Task<IActionResult> Create(UpsertBankAccountCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertBankAccountCommand cmd)
    { cmd.Id = id; await Mediator.Send(cmd); return NoContent(); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var account = await db.BankAccounts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);
        if (account is null) return NotFound();

        var hasTransactions = await db.BankTransactions.AnyAsync(t =>
            !t.IsDeleted && (t.BankAccountId == id || t.DestinationBankAccountId == id), ct);
        var hasCustomerPayments = await db.CustomerPayments.AnyAsync(p => !p.IsDeleted && p.BankAccountId == id, ct);
        var hasSupplierPayments = await db.SupplierPayments.AnyAsync(p => !p.IsDeleted && p.BankAccountId == id, ct);
        var hasCheques = await db.Cheques.AnyAsync(ch => !ch.IsDeleted && ch.BankAccountId == id, ct);

        if (hasTransactions || hasCustomerPayments || hasSupplierPayments || hasCheques)
            return BadRequest(new { message = "Bank account has financial history. Reverse/delete its related transactions before deleting the account." });

        account.IsActive = false;
        account.IsDeleted = true;
        account.ModifiedAt = DateTime.UtcNow;
        account.ModifiedBy = user.UserId;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/commissions")]
public class CommissionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? salespersonId, [FromQuery] CommissionStatus? status)
        => Ok(await Mediator.Send(new GetSalesCommissionsQuery { SalespersonId = salespersonId, Status = status }));
}
