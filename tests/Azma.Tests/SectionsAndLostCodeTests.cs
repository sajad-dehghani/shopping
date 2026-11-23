using Azma.Application.Admin;
using Azma.Application.Checkout;
using Azma.Application.Support;
using Azma.Domain.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Azma.Tests;

public class SectionsAndLostCodeTests : IDisposable
{
    private readonly TestHost _h = new();

    private TicketService Tickets => new(_h.DbFactory, _h.Clock, _h.Audit, NullLogger<TicketService>.Instance);
    private CheckoutService Checkout => new(_h.DbFactory, _h.Clock, _h.Settings, NullLogger<CheckoutService>.Instance);

    [Fact]
    public async Task Lost_code_ticket_is_created_without_an_order_and_staff_can_list_orders_by_mobile()
    {
        var product = _h.AddProduct();
        var order = await Checkout.CreateProductOrderAsync(product.Id, new ProductCheckoutInput { FullName = "سارا تست", Mobile = "09121234567", Quantity = 1 });
        Assert.True(order.Succeeded, order.Error);

        var r = await Tickets.CreateAsync(new NewTicketInput
        {
            LostCode = true,
            FullName = "سارا تست",
            Mobile = "۰۹۱۲۱۲۳۴۵۶۷",
            Subject = TicketService.LostCodeSubject,
            Message = "کد رهگیری را گم کرده‌ام.",
        });
        Assert.True(r.Succeeded, r.Error);

        using var db = _h.NewContext();
        var ticket = await db.Tickets.SingleAsync();
        Assert.Null(ticket.OrderId);
        Assert.Equal(TicketService.LostCodeSubject, ticket.Subject);
        Assert.Equal(TicketStatus.Open, ticket.Status);

        var orders = await Tickets.OrdersForMobileAsync(ticket.Mobile);
        Assert.Single(orders);
        Assert.Equal(product.Name, orders[0].FirstItem);
    }

    [Fact]
    public async Task Normal_ticket_still_requires_a_code()
    {
        var r = await Tickets.CreateAsync(new NewTicketInput { FullName = "سارا تست", Mobile = "09121234567", Subject = "سایر موارد", Message = "سلام وقت بخیر" });
        Assert.False(r.Succeeded);
    }

    [Fact]
    public async Task Disabled_shop_rejects_product_checkout()
    {
        using (var db = _h.NewContext())
        {
            db.SiteSettings.Single().ShopEnabled = false;
            db.SaveChanges();
        }
        var product = _h.AddProduct();
        var r = await Checkout.CreateProductOrderAsync(product.Id, new ProductCheckoutInput { FullName = "سارا تست", Mobile = "09121234567", Quantity = 1 });
        Assert.False(r.Succeeded);
    }

    [Theory]
    [InlineData("/courses", true)]
    [InlineData("https://example.ir/x", true)]
    [InlineData(null, true)]
    [InlineData("//evil.com", false)]
    [InlineData("/\\evil.com", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("http://example.ir", false)]
    public void Slide_links_must_be_local_or_https(string? url, bool ok) => Assert.Equal(ok, ContentAdminService.IsSafeLink(url));

    public void Dispose() => _h.Dispose();
}
