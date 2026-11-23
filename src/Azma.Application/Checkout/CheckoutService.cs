using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Text.Json;
using Azma.Application.Abstractions;
using Azma.Application.Catalog;
using Azma.Application.Common;
using Azma.Domain.Catalog;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azma.Application.Checkout;

/// <summary>
/// Creates orders for courses and products. Prices and totals are always read from the
/// database here — nothing price-related is taken from the client.
/// </summary>
public sealed class CheckoutService(
    IAppDbContextFactory dbFactory,
    ISiteClock clock,
    SiteSettingsService settingsService,
    ILogger<CheckoutService> logger)
{
    public async Task<Result<CheckoutResult>> CreateCourseOrderAsync(int courseId, CourseCheckoutInput input, CancellationToken ct = default)
    {
        if (ValidateCustomer(input, out var mobile) is { } customerError) return Result<CheckoutResult>.Fail(customerError);
        var settings = await settingsService.GetAsync(ct);

        await using var db = await dbFactory.CreateAsync(ct);
        // Serializable: capacity check + insert must not interleave with another checkout for the same course.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var course = await db.Courses.Include(c => c.RegistrationFields).FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null) return Result<CheckoutResult>.Fail("دوره موردنظر یافت نشد.");

        var now = clock.UtcNow;
        var used = await db.Enrollments.Where(e => e.CourseId == course.Id).CountAsync(SeatQueries.OccupiesSeat(now), ct);
        var state = course.GetRegistrationState(clock.Today, used);
        if (state != RegistrationState.Open) return Result<CheckoutResult>.Fail(DisplayNames.Of(state) + ".");

        if (!course.AllowDuplicateEnrollment
            && await db.Enrollments.AnyAsync(e => e.CourseId == course.Id && e.Mobile == mobile && e.Status == EnrollmentStatus.Confirmed, ct))
        {
            return Result<CheckoutResult>.Fail("با این شماره موبایل قبلاً در این دوره ثبت‌نام قطعی انجام شده است.");
        }

        var fieldsResult = ValidateExtraFields(course.RegistrationFields, input.Fields);
        if (!fieldsResult.Succeeded) return Result<CheckoutResult>.Fail(fieldsResult.Error!);

        var price = course.FinalPrice;
        var order = NewOrder(input, mobile, price, now);
        order.OrderNumber = await OrderNumberGenerator.NextAsync(db, clock.LocalNow, ct);
        var item = new OrderItem
        {
            ItemType = OrderItemType.Course,
            CourseId = course.Id,
            Title = course.Title,
            UnitPrice = course.Price,
            DiscountPerUnit = course.Price - price,
            Quantity = 1,
            TotalPrice = price,
        };
        order.Items.Add(item);
        order.Enrollments.Add(new Enrollment
        {
            CourseId = course.Id,
            OrderItem = item,
            FullName = order.CustomerName,
            Mobile = mobile,
            Email = order.Email,
            Notes = order.CustomerNote,
            ExtraFieldsJson = fieldsResult.Value!.Count > 0 ? JsonSerializer.Serialize(fieldsResult.Value) : null,
            Status = EnrollmentStatus.PendingPayment,
            ReservedUntil = now.AddMinutes(settings.SeatReservationMinutes),
            CreatedAt = now,
        });

        if (price == 0) MarkFreeOrderPaid(order, now);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Order {OrderNumber} created for course {CourseId} ({Amount} Toman)", order.OrderNumber, course.Id, price);
        return Result<CheckoutResult>.Ok(new CheckoutResult(order.PublicId, order.OrderNumber, order.TotalAmount, price == 0));
    }

    public async Task<Result<CheckoutResult>> CreateProductOrderAsync(int productId, ProductCheckoutInput input, CancellationToken ct = default)
    {
        if (ValidateCustomer(input, out var mobile) is { } customerError) return Result<CheckoutResult>.Fail(customerError);
        if (input.Quantity is < 1 or > 20) return Result<CheckoutResult>.Fail("تعداد نامعتبر است.");

        await using var db = await dbFactory.CreateAsync(ct);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null) return Result<CheckoutResult>.Fail("محصول موردنظر یافت نشد.");
        if (!product.IsPurchasable(input.Quantity))
            return Result<CheckoutResult>.Fail(product.Stock is { } s && s > 0 && s < input.Quantity
                ? $"موجودی این محصول فقط {Persian.Digits(s)} عدد است."
                : "این محصول در حال حاضر قابل خرید نیست.");

        if (product.ProductType == ProductType.Physical && string.IsNullOrWhiteSpace(input.ShippingAddress))
            return Result<CheckoutResult>.Fail("برای محصولات فیزیکی، وارد کردن آدرس ارسال الزامی است.");

        var now = clock.UtcNow;
        var unit = product.FinalPrice;
        var total = checked(unit * input.Quantity);
        var order = NewOrder(input, mobile, total, now);
        order.OrderNumber = await OrderNumberGenerator.NextAsync(db, clock.LocalNow, ct);
        if (product.ProductType == ProductType.Physical)
        {
            order.ShippingAddress = Persian.NormalizeText(input.ShippingAddress);
            order.PostalCode = string.IsNullOrWhiteSpace(input.PostalCode) ? null : Persian.NormalizeDigits(input.PostalCode.Trim());
        }
        order.Items.Add(new OrderItem
        {
            ItemType = OrderItemType.Product,
            ProductId = product.Id,
            Title = product.Name,
            UnitPrice = product.Price,
            DiscountPerUnit = product.Price - unit,
            Quantity = input.Quantity,
            TotalPrice = total,
        });

        if (total == 0)
        {
            MarkFreeOrderPaid(order, now);
            if (product.Stock is { } stock) product.Stock = Math.Max(0, stock - input.Quantity);
        }
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Order {OrderNumber} created for product {ProductId} x{Qty} ({Amount} Toman)", order.OrderNumber, product.Id, input.Quantity, total);
        return Result<CheckoutResult>.Ok(new CheckoutResult(order.PublicId, order.OrderNumber, order.TotalAmount, total == 0));
    }

    /// <summary>Public order page data (by unguessable public id).</summary>
    public async Task<Order?> GetOrderAsync(Guid publicId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Orders.AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Transactions.OrderByDescending(t => t.Id))
            .Include(o => o.Receipts.OrderByDescending(r => r.Id))
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.PublicId == publicId, ct);
    }

    /// <summary>Order tracking: both the order number and the mobile used must match.</summary>
    public async Task<Guid?> FindOrderAsync(string orderNumber, string mobile, CancellationToken ct = default)
    {
        var m = Persian.NormalizeMobile(mobile);
        var number = Persian.NormalizeDigits(orderNumber ?? "").Trim();
        if (m is null || number.Length is < 6 or > 20) return null;
        await using var db = await dbFactory.CreateAsync(ct);
        var id = await db.Orders.Where(o => (o.OrderNumber == number || o.TrackingCode == number) && o.Mobile == m)
            .Select(o => (Guid?)o.PublicId).FirstOrDefaultAsync(ct);
        return id;
    }

    private static string? ValidateCustomer(CustomerInput input, out string mobile)
    {
        mobile = "";
        var ctx = new ValidationContext(input);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, ctx, errors, validateAllProperties: true))
            return errors[0].ErrorMessage;
        var normalized = Persian.NormalizeMobile(input.Mobile);
        if (normalized is null) return "شماره موبایل معتبر نیست (مثال: ۰۹۱۲۱۲۳۴۵۶۷).";
        mobile = normalized;
        return null;
    }

    private static Order NewOrder(CustomerInput input, string mobile, long total, DateTime now) => new()
    {
        CustomerName = Persian.NormalizeText(input.FullName),
        Mobile = mobile,
        Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim().ToLowerInvariant(),
        CustomerNote = string.IsNullOrWhiteSpace(input.Notes) ? null : Persian.NormalizeText(input.Notes),
        TotalAmount = total,
        Status = OrderStatus.PendingPayment,
        CreatedAt = now,
        StatusHistory =
        [
            new OrderStatusHistory { FromStatus = OrderStatus.PendingPayment, ToStatus = OrderStatus.PendingPayment, Reason = "ثبت سفارش", ChangedAt = now },
        ],
    };

    private static void MarkFreeOrderPaid(Order order, DateTime now)
    {
        order.ChangeStatus(OrderStatus.Paid, now, "سفارش رایگان - بدون نیاز به پرداخت");
        order.PaidAt = now;
        order.TrackingCode = order.OrderNumber;
        foreach (var e in order.Enrollments)
        {
            e.Status = EnrollmentStatus.Confirmed;
            e.ConfirmedAt = now;
            e.ReservedUntil = null;
        }
    }

    public static Result<List<ExtraFieldValue>> ValidateExtraFields(IEnumerable<CourseRegistrationField> fields, IReadOnlyDictionary<string, string?> values)
    {
        var result = new List<ExtraFieldValue>();
        foreach (var field in fields.OrderBy(f => f.SortOrder))
        {
            values.TryGetValue(field.Id.ToString(CultureInfo.InvariantCulture), out var raw);
            var value = Persian.NormalizeText(raw);
            if (value.Length > 1000) return Result<List<ExtraFieldValue>>.Fail($"مقدار «{field.Label}» بیش از حد طولانی است.");

            switch (field.FieldType)
            {
                case RegistrationFieldType.Checkbox:
                    var isChecked = value is "true" or "on" or "1";
                    if (field.IsRequired && !isChecked) return Result<List<ExtraFieldValue>>.Fail($"تأیید «{field.Label}» الزامی است.");
                    result.Add(new ExtraFieldValue(field.Label, isChecked ? "بله" : "خیر"));
                    continue;
                case RegistrationFieldType.Number when value.Length > 0:
                    value = Persian.NormalizeDigits(value);
                    if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                        return Result<List<ExtraFieldValue>>.Fail($"مقدار «{field.Label}» باید عدد باشد.");
                    break;
                case RegistrationFieldType.Select when value.Length > 0:
                    if (!field.OptionItems.Contains(value))
                        return Result<List<ExtraFieldValue>>.Fail($"گزینه انتخاب‌شده برای «{field.Label}» معتبر نیست.");
                    break;
            }

            if (field.IsRequired && value.Length == 0) return Result<List<ExtraFieldValue>>.Fail($"تکمیل «{field.Label}» الزامی است.");
            if (value.Length > 0) result.Add(new ExtraFieldValue(field.Label, value));
        }
        return Result<List<ExtraFieldValue>>.Ok(result);
    }
}
