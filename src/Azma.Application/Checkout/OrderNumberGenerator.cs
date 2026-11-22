using System.Globalization;
using System.Security.Cryptography;
using Azma.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Azma.Application.Checkout;

/// <summary>Order numbers look like 050901-48213 (Jalali yyMMdd + 5 random digits): short enough to read over the phone.</summary>
public static class OrderNumberGenerator
{
    public static async Task<string> NextAsync(IAppDbContext db, DateTime localNow, CancellationToken ct)
    {
        var pc = new PersianCalendar();
        var prefix = $"{pc.GetYear(localNow) % 100:00}{pc.GetMonth(localNow):00}{pc.GetDayOfMonth(localNow):00}";
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var number = $"{prefix}-{RandomNumberGenerator.GetInt32(10000, 99999)}";
            if (!await db.Orders.AnyAsync(o => o.OrderNumber == number, ct)) return number;
        }
        return $"{prefix}-{RandomNumberGenerator.GetInt32(100000, 999999)}";
    }
}
