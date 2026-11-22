using Azma.Application.Checkout;
using Azma.Application.Payments;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Azma.Tests;

public class PaymentFlowTests : IDisposable
{
    private readonly TestHost _h = new();
    private CheckoutService Checkout => new(_h.DbFactory, _h.Clock, _h.Settings, NullLogger<CheckoutService>.Instance);
    private PaymentService Payments => new(_h.DbFactory, _h.Gateway, _h.Clock, _h.Settings, _h.Audit, NullLogger<PaymentService>.Instance);

    private static CourseCheckoutInput Customer(string mobile = "09121234567") => new() { FullName = "علی تست", Mobile = mobile };

    private async Task<(Guid OrderId, string Authority)> StartCoursePaymentAsync(int courseId, string mobile = "09121234567")
    {
        var order = await Checkout.CreateCourseOrderAsync(courseId, Customer(mobile));
        Assert.True(order.Succeeded, order.Error);
        var start = await Payments.StartPaymentAsync(order.Value!.OrderPublicId, "https://site.test/payment/zarinpal/callback");
        Assert.True(start.Succeeded, start.Error);
        var authority = start.Value!.Split('/').Last();
        return (order.Value.OrderPublicId, authority);
    }

    [Fact]
    public async Task Successful_payment_marks_order_paid_and_confirms_enrollment()
    {
        var course = _h.AddCourse(price: 1_500_000, discount: 1_200_000);
        var (orderId, authority) = await StartCoursePaymentAsync(course.Id);

        // Amount sent to the gateway is the server-side final price converted to Rial.
        Assert.Equal(12_000_000, _h.Gateway.Requests.Single().AmountRial);

        var result = await Payments.HandleCallbackAsync(authority, "OK");
        Assert.True(result.Succeeded);
        Assert.Equal(PaymentStatus.Succeeded, result.Value!.Status);

        using var db = _h.NewContext();
        var order = await db.Orders.Include(o => o.Enrollments).Include(o => o.Transactions).SingleAsync(o => o.PublicId == orderId);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal("123456789", order.TrackingCode);
        Assert.All(order.Enrollments, e => Assert.Equal(EnrollmentStatus.Confirmed, e.Status));
        Assert.Equal(12_000_000, _h.Gateway.Verifies.Single().Amount);
    }

    [Fact]
    public async Task Callback_replay_does_not_verify_twice()
    {
        var course = _h.AddCourse();
        var (_, authority) = await StartCoursePaymentAsync(course.Id);

        await Payments.HandleCallbackAsync(authority, "OK");
        var second = await Payments.HandleCallbackAsync(authority, "OK");

        Assert.Equal(PaymentStatus.Succeeded, second.Value!.Status);
        Assert.Single(_h.Gateway.Verifies);
    }

    [Fact]
    public async Task Status_OK_in_callback_is_not_trusted_without_verify()
    {
        var course = _h.AddCourse();
        var (orderId, authority) = await StartCoursePaymentAsync(course.Id);
        _h.Gateway.VerifyResult = (_, _) => new GatewayVerifyResult(GatewayVerifyOutcome.Failed, -51, "unpaid");

        var result = await Payments.HandleCallbackAsync(authority, "OK");

        Assert.Equal(PaymentStatus.Failed, result.Value!.Status);
        using var db = _h.NewContext();
        Assert.Equal(OrderStatus.PaymentFailed, (await db.Orders.SingleAsync(o => o.PublicId == orderId)).Status);
    }

    [Fact]
    public async Task Cancelled_payment_can_be_retried()
    {
        var course = _h.AddCourse();
        var (orderId, authority) = await StartCoursePaymentAsync(course.Id);
        _h.Gateway.VerifyResult = (_, _) => new GatewayVerifyResult(GatewayVerifyOutcome.Failed, -51, "cancelled");
        var cancelled = await Payments.HandleCallbackAsync(authority, "NOK");
        Assert.Equal(PaymentStatus.Cancelled, cancelled.Value!.Status);

        _h.Gateway.VerifyResult = (_, _) => new GatewayVerifyResult(GatewayVerifyOutcome.Verified, 100, "ok", "999");
        var retry = await Payments.StartPaymentAsync(orderId, "https://site.test/cb");
        Assert.True(retry.Succeeded, retry.Error);
        var ok = await Payments.HandleCallbackAsync(retry.Value!.Split('/').Last(), "OK");
        Assert.Equal(PaymentStatus.Succeeded, ok.Value!.Status);

        using var db = _h.NewContext();
        var order = await db.Orders.Include(o => o.Transactions).SingleAsync(o => o.PublicId == orderId);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(2, order.Transactions.Count);
    }

    [Fact]
    public async Task Network_error_on_verify_leaves_order_unknown_and_recheck_resolves_it()
    {
        var course = _h.AddCourse();
        var (orderId, authority) = await StartCoursePaymentAsync(course.Id);
        _h.Gateway.VerifyResult = (_, _) => new GatewayVerifyResult(GatewayVerifyOutcome.Unknown, null, "timeout");

        var first = await Payments.HandleCallbackAsync(authority, "OK");
        Assert.Equal(PaymentStatus.Unknown, first.Value!.Status);

        int txId;
        using (var db = _h.NewContext())
        {
            Assert.Equal(OrderStatus.PaymentUnknown, (await db.Orders.SingleAsync(o => o.PublicId == orderId)).Status);
            txId = (await db.PaymentTransactions.SingleAsync(t => t.Authority == authority)).Id;
        }

        _h.Gateway.VerifyResult = (_, _) => new GatewayVerifyResult(GatewayVerifyOutcome.AlreadyVerified, 101, "verified", "555");
        var recheck = await Payments.RecheckAsync(txId, new Application.Abstractions.AdminActor("u1", "admin"));
        Assert.Equal(PaymentStatus.Succeeded, recheck.Value!.Status);

        using var db2 = _h.NewContext();
        Assert.Equal(OrderStatus.Paid, (await db2.Orders.SingleAsync(o => o.PublicId == orderId)).Status);
    }

    [Fact]
    public async Task Unknown_authority_is_rejected()
    {
        var r = await Payments.HandleCallbackAsync("NOT-A-REAL-AUTHORITY", "OK");
        Assert.False(r.Succeeded);
        Assert.Empty(_h.Gateway.Verifies);
    }

    [Fact]
    public async Task Gateway_request_failure_keeps_order_pending()
    {
        var course = _h.AddCourse();
        var order = await Checkout.CreateCourseOrderAsync(course.Id, Customer());
        _h.Gateway.FailRequests = true;
        var start = await Payments.StartPaymentAsync(order.Value!.OrderPublicId, "https://site.test/cb");
        Assert.False(start.Succeeded);
        using var db = _h.NewContext();
        var tx = await db.PaymentTransactions.SingleAsync();
        Assert.Equal(PaymentStatus.RequestFailed, tx.Status);
        Assert.Equal(OrderStatus.PendingPayment, (await db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Capacity_counts_confirmed_and_reserved_seats()
    {
        var course = _h.AddCourse(capacity: 1);
        Assert.True((await Checkout.CreateCourseOrderAsync(course.Id, Customer("09120000001"))).Succeeded);
        var full = await Checkout.CreateCourseOrderAsync(course.Id, Customer("09120000002"));
        Assert.False(full.Succeeded);

        // After the reservation window expires the seat is released.
        _h.Clock.UtcNow = _h.Clock.UtcNow.AddMinutes(31);
        Assert.True((await Checkout.CreateCourseOrderAsync(course.Id, Customer("09120000003"))).Succeeded);
    }

    [Fact]
    public async Task Duplicate_enrollment_is_blocked_unless_allowed()
    {
        var strict = _h.AddCourse();
        var (_, authority) = await StartCoursePaymentAsync(strict.Id, "09125550000");
        await Payments.HandleCallbackAsync(authority, "OK");
        Assert.False((await Checkout.CreateCourseOrderAsync(strict.Id, Customer("09125550000"))).Succeeded);
        Assert.True((await Checkout.CreateCourseOrderAsync(strict.Id, Customer("09125550001"))).Succeeded);

        var relaxed = _h.AddCourse(allowDuplicate: true);
        var (_, a2) = await StartCoursePaymentAsync(relaxed.Id, "09125550000");
        await Payments.HandleCallbackAsync(a2, "OK");
        Assert.True((await Checkout.CreateCourseOrderAsync(relaxed.Id, Customer("09125550000"))).Succeeded);
    }

    [Fact]
    public async Task Free_course_is_confirmed_without_gateway()
    {
        var course = _h.AddCourse(price: 0);
        var r = await Checkout.CreateCourseOrderAsync(course.Id, Customer());
        Assert.True(r.Value!.IsFree);
        Assert.Empty(_h.Gateway.Requests);
        using var db = _h.NewContext();
        Assert.Equal(EnrollmentStatus.Confirmed, (await db.Enrollments.SingleAsync()).Status);
    }

    [Fact]
    public async Task Product_payment_decrements_stock_and_physical_requires_address()
    {
        var physical = _h.AddProduct(price: 300_000, stock: 5, type: Domain.Catalog.ProductType.Physical);
        var noAddress = await Checkout.CreateProductOrderAsync(physical.Id, new ProductCheckoutInput { FullName = "خریدار", Mobile = "09121234567", Quantity = 2 });
        Assert.False(noAddress.Succeeded);

        var order = await Checkout.CreateProductOrderAsync(physical.Id, new ProductCheckoutInput { FullName = "خریدار", Mobile = "09121234567", Quantity = 2, ShippingAddress = "تهران" });
        Assert.Equal(600_000, order.Value!.TotalAmount);
        var start = await Payments.StartPaymentAsync(order.Value.OrderPublicId, "https://site.test/cb");
        await Payments.HandleCallbackAsync(start.Value!.Split('/').Last(), "OK");

        using var db = _h.NewContext();
        Assert.Equal(3, (await db.Products.SingleAsync(p => p.Id == physical.Id)).Stock);
        Assert.Equal(6_000_000, _h.Gateway.Verifies.Single().Amount);

        var tooMany = await Checkout.CreateProductOrderAsync(physical.Id, new ProductCheckoutInput { FullName = "خریدار", Mobile = "09121234567", Quantity = 4, ShippingAddress = "تهران" });
        Assert.False(tooMany.Succeeded);
    }

    public void Dispose() => _h.Dispose();
}
