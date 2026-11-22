namespace Azma.Domain.Common;

/// <summary>
/// All prices in the database are stored as whole Toman (long).
/// The ZarinPal gateway is called in Rial, so conversion happens only here.
/// </summary>
public static class Money
{
    public const int RialPerToman = 10;

    public static long TomanToRial(long toman) => checked(toman * RialPerToman);

    /// <summary>Final price after an optional discount price.</summary>
    public static long FinalPrice(long price, long? discountPrice)
        => discountPrice is > 0 && discountPrice < price ? discountPrice.Value : price;

    public static int DiscountPercent(long price, long? discountPrice)
    {
        var final = FinalPrice(price, discountPrice);
        if (price <= 0 || final >= price) return 0;
        return (int)Math.Round((price - final) * 100m / price);
    }
}
