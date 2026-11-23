using Azma.Application.Abstractions;
using Azma.Application.Checkout;
using Azma.Application.Payments;
using Azma.Application.Support;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Azma.Tests;

public class ReceiptAndTicketTests : IDisposable
{
    private readonly TestHost _h = new();
    private readonly FakeFiles _files = new();
    private static readonly AdminActor Admin = new("1", "admin@test");
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];

    public ReceiptAndTicketTests()
    {
        using var db = _h.NewContext();
        var s = db.SiteSettings.Single();
        s.CardToCardEnabled = true;
        s.CardNumber = "6037000000000000";
        db.SaveChanges();
    }

    private CheckoutService Checkout => new(_h.DbFactory, _h.Clock, _h.Settings, NullLogger<CheckoutService>.Instance);
    private ReceiptService Receipts => new(_h.DbFactory, _files, _h.Clock, _h.Settings, _h.Audit, NullLogger<ReceiptService>.Instance);
    private TicketService Tickets => new(_h.DbFactory, _h.Clock, _h.Audit, NullLogger<TicketService>.Instance);

    private async Task<Guid> NewProductOrderAsync(int productId, int qty = 2)
    {
        var r = await Checkout.CreateProductOrderAsync(productId, new ProductCheckoutInput { FullName = "سارا تست", Mobile = "09121234567", Quantity = qty });
        Assert.True(r.Succeeded, r.Error);
        return r.Value!.OrderPublicId;
    }

    private Task<Azma.Application.Common.Result> SubmitAsync(Guid orderId, string name = "سارا تست", string mobile = "09121234567")
        => Receipts.SubmitAsync(orderId, new ReceiptInput { FullName = name, Mobile = mobile }, new MemoryStream(Png), "r.png");

    [Fact]
    public async Task Receipt_requires_name_and_valid_mobile()
    {
        var product = _h.AddProduct();
        var orderId = await NewProductOrderAsync(product.Id);

        Assert.False((await SubmitAsync(orderId, name: "")).Succeeded);
        Assert.False((await SubmitAsync(orderId, mobile: "12345")).Succeeded);
        Assert.Empty(_files.Saved);
    }

    [Fact]
    public async Task Approved_receipt_marks_order_paid_with_tracking_code_and_decreases_stock()
    {
        var product = _h.AddProduct(stock: 10);
        var orderId = await NewProductOrderAsync(product.Id, qty: 2);

        var submit = await SubmitAsync(orderId);
        Assert.True(submit.Succeeded, submit.Error);
        int receiptId;
        using (var db = _h.NewContext())
        {
            var order = await db.Orders.Include(o => o.Receipts).SingleAsync(o => o.PublicId == orderId);
            Assert.Equal(OrderStatus.AwaitingApproval, order.Status);
            Assert.Null(order.TrackingCode); // no tracking code before admin approval
            receiptId = order.Receipts.Single().Id;
        }
        // A second receipt cannot be sent while the first is under review.
        Assert.False((await SubmitAsync(orderId)).Succeeded);

        var approve = await Receipts.ApproveAsync(receiptId, Admin);
        Assert.True(approve.Succeeded, approve.Error);

        using (var db = _h.NewContext())
        {
            var order = await db.Orders.Include(o => o.Receipts).SingleAsync(o => o.PublicId == orderId);
            Assert.Equal(OrderStatus.Paid, order.Status);
            Assert.Equal(approve.Value, order.TrackingCode);
            Assert.Equal(10, order.TrackingCode!.Length);
            Assert.Equal(ReceiptStatus.Approved, order.Receipts.Single().Status);
            Assert.Equal(8, (await db.Products.SingleAsync(p => p.Id == product.Id)).Stock);
        }
        // The tracking code works for order lookup.
        Assert.Equal(orderId, await Checkout.FindOrderAsync(approve.Value!, "09121234567"));
        Assert.False((await Receipts.ApproveAsync(receiptId, Admin)).Succeeded);
    }

    [Fact]
    public async Task Rejected_receipt_returns_order_to_pending_and_allows_resubmit()
    {
        var product = _h.AddProduct();
        var orderId = await NewProductOrderAsync(product.Id);
        await SubmitAsync(orderId);
        int receiptId;
        using (var db = _h.NewContext()) receiptId = (await db.PaymentReceipts.SingleAsync()).Id;

        Assert.False((await Receipts.RejectAsync(receiptId, "", Admin)).Succeeded); // reason is mandatory
        Assert.True((await Receipts.RejectAsync(receiptId, "مبلغ واریزی کمتر است", Admin)).Succeeded);

        using (var db = _h.NewContext())
            Assert.Equal(OrderStatus.PendingPayment, (await db.Orders.SingleAsync(o => o.PublicId == orderId)).Status);
        Assert.True((await SubmitAsync(orderId)).Succeeded);
    }

    [Fact]
    public async Task Ticket_needs_matching_order_code_and_mobile()
    {
        var product = _h.AddProduct();
        var orderId = await NewProductOrderAsync(product.Id);
        string number;
        using (var db = _h.NewContext()) number = (await db.Orders.SingleAsync(o => o.PublicId == orderId)).OrderNumber;

        var input = new NewTicketInput { Code = number, FullName = "سارا تست", Mobile = "09129999999", Subject = "پیگیری وضعیت سفارش", Message = "سفارش من کی ارسال می‌شود؟" };
        Assert.False((await Tickets.CreateAsync(input)).Succeeded); // wrong mobile

        input.Mobile = "۰۹۱۲۱۲۳۴۵۶۷"; // Persian digits are normalized
        var created = await Tickets.CreateAsync(input);
        Assert.True(created.Succeeded, created.Error);

        using var db2 = _h.NewContext();
        var ticket = await db2.Tickets.Include(t => t.Messages).SingleAsync();
        Assert.Equal(created.Value, ticket.PublicId);
        Assert.Single(ticket.Messages);
        Assert.Equal(created.Value, await Tickets.FindAsync(ticket.TicketNumber, "09121234567"));

        Assert.True((await Tickets.StaffReplyAsync(ticket.Id, "در حال ارسال است.", Admin, close: false)).Succeeded);
        Assert.True((await Tickets.CustomerReplyAsync(ticket.PublicId, new TicketReplyInput { Message = "ممنون" })).Succeeded);
        Assert.Equal(3, (await Tickets.GetPublicAsync(ticket.PublicId))!.Messages.Count);
    }

    public void Dispose() => _h.Dispose();

    private sealed class FakeFiles : IFileStorage
    {
        public List<string> Saved { get; } = [];
        public Task<string> SaveImageAsync(Stream content, string originalFileName, string folder, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string? relativePath) => Task.CompletedTask;
        public Task<string> SavePrivateImageAsync(Stream content, string originalFileName, string folder, CancellationToken cancellationToken = default)
        {
            var path = $"private/{folder}/{Guid.NewGuid():N}.png";
            Saved.Add(path);
            return Task.FromResult(path);
        }
        public (Stream Content, string ContentType)? OpenPrivate(string? privatePath) => null;
    }
}
