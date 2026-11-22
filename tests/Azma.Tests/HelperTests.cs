using Azma.Application.Checkout;
using Azma.Application.Common;
using Azma.Domain.Catalog;
using Azma.Domain.Common;
using Azma.Infrastructure.Payments.ZarinPal;

namespace Azma.Tests;

public class HelperTests
{
    [Theory]
    [InlineData("09121234567", "09121234567")]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷", "09121234567")]
    [InlineData("+98 912 123 4567", "09121234567")]
    [InlineData("9121234567", "09121234567")]
    [InlineData("0912123456", null)]
    [InlineData("02112345678", null)]
    public void NormalizeMobile(string input, string? expected) => Assert.Equal(expected, Persian.NormalizeMobile(input));

    [Fact]
    public void Jalali_round_trip()
    {
        Assert.True(Persian.TryParseJalali("1405/09/01", out var d));
        Assert.Equal(new DateOnly(2026, 11, 22), d);
        Assert.Equal("1405/09/01", Persian.ToJalaliInput(d));
        Assert.True(Persian.TryParseJalali("۱۴۰۵/۰۱/۰۱", out _));
        Assert.False(Persian.TryParseJalali("1405/13/01", out _));
        Assert.False(Persian.TryParseJalali("1405/12/31", out _)); // 1405 is not a leap year
    }

    [Fact]
    public void Money_conversion_and_discount()
    {
        Assert.Equal(52_000_000, Money.TomanToRial(5_200_000));
        Assert.Equal(800, Money.FinalPrice(1000, 800));
        Assert.Equal(1000, Money.FinalPrice(1000, 1200)); // invalid discount ignored
        Assert.Equal(1000, Money.FinalPrice(1000, 0));
        Assert.Equal(20, Money.DiscountPercent(1000, 800));
    }

    [Fact]
    public void Registration_state_rules()
    {
        var c = new Course { Status = PublishStatus.Published, Capacity = 2, RegistrationStartsOn = new(2026, 1, 10), RegistrationEndsOn = new(2026, 1, 20) };
        Assert.Equal(RegistrationState.NotStarted, c.GetRegistrationState(new(2026, 1, 9), 0));
        Assert.Equal(RegistrationState.Open, c.GetRegistrationState(new(2026, 1, 20), 1));
        Assert.Equal(RegistrationState.Full, c.GetRegistrationState(new(2026, 1, 15), 2));
        Assert.Equal(RegistrationState.Closed, c.GetRegistrationState(new(2026, 1, 21), 0));
        c.Status = PublishStatus.Draft;
        Assert.Equal(RegistrationState.NotPublished, c.GetRegistrationState(new(2026, 1, 15), 0));
    }

    [Fact]
    public void Extra_fields_validation()
    {
        var fields = new[]
        {
            new CourseRegistrationField { Id = 1, Label = "سطح", FieldType = RegistrationFieldType.Select, Options = "مبتدی\nپیشرفته", IsRequired = true },
            new CourseRegistrationField { Id = 2, Label = "سن", FieldType = RegistrationFieldType.Number },
        };
        Assert.False(CheckoutService.ValidateExtraFields(fields, new Dictionary<string, string?>()).Succeeded);
        Assert.False(CheckoutService.ValidateExtraFields(fields, new Dictionary<string, string?> { ["1"] = "هک" }).Succeeded);
        Assert.False(CheckoutService.ValidateExtraFields(fields, new Dictionary<string, string?> { ["1"] = "مبتدی", ["2"] = "abc" }).Succeeded);
        var ok = CheckoutService.ValidateExtraFields(fields, new Dictionary<string, string?> { ["1"] = "مبتدی", ["2"] = "۲۵" });
        Assert.True(ok.Succeeded);
        Assert.Equal("25", ok.Value![1].Value);
    }

    [Fact]
    public void ZarinPal_parses_success_and_error_shapes()
    {
        var success = ZarinPalGateway.Parse("""{"data":{"code":100,"message":"Verified","card_pan":"502229******5995","ref_id":201,"fee":0},"errors":[]}""");
        Assert.Equal(100, success!.Code);
        Assert.Equal("201", success.RefId);
        Assert.Equal("502229******5995", success.CardPan);

        var error = ZarinPalGateway.Parse("""{"data":[],"errors":{"code":-51,"message":"Session is not valid","validations":[]}}""");
        Assert.Equal(-51, error!.Code);

        Assert.Null(ZarinPalGateway.Parse("<html>bad gateway</html>"));
    }
}
