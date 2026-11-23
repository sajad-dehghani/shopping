using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Azma.Application.Abstractions;
using Azma.Application.Catalog;
using Azma.Application.Common;
using Azma.Domain.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azma.Application.Support;

public sealed class NewTicketInput
{
    /// <summary>Tracking code or order number. Not needed when <see cref="LostCode"/> is set.</summary>
    [StringLength(30)]
    public string? Code { get; set; }

    /// <summary>The customer forgot their tracking code: the ticket is opened without an order and support looks it up by mobile.</summary>
    public bool LostCode { get; set; }

    [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "نام و نام خانوادگی باید بین ۳ تا ۱۰۰ حرف باشد.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
    [StringLength(20)]
    public string Mobile { get; set; } = "";

    [Required(ErrorMessage = "موضوع تیکت را وارد کنید.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "موضوع باید بین ۳ تا ۱۵۰ حرف باشد.")]
    public string Subject { get; set; } = "";

    [Required(ErrorMessage = "متن پیام را بنویسید.")]
    [StringLength(4000, MinimumLength = 5, ErrorMessage = "متن پیام باید بین ۵ تا ۴۰۰۰ حرف باشد.")]
    public string Message { get; set; } = "";
}

public sealed class TicketReplyInput
{
    [Required(ErrorMessage = "متن پیام را بنویسید.")]
    [StringLength(4000, MinimumLength = 2, ErrorMessage = "متن پیام باید بین ۲ تا ۴۰۰۰ حرف باشد.")]
    public string Message { get; set; } = "";
}

public sealed record MobileOrderRow(int Id, string OrderNumber, string? TrackingCode, string CustomerName,
    Azma.Domain.Sales.OrderStatus Status, long TotalAmount, DateTime CreatedAt, string? FirstItem);

public sealed record TicketRow(int Id, string TicketNumber, string Subject, string FullName, string Mobile, string? OrderNumber,
    TicketStatus Status, DateTime CreatedAt, DateTime LastActivityAt, int MessageCount);

/// <summary>Support tickets for guest customers. A ticket must reference an existing order (by tracking code or order number) whose mobile matches.</summary>
public sealed class TicketService(IAppDbContextFactory dbFactory, ISiteClock clock, IAuditLogger audit, ILogger<TicketService> logger)
{
    public const string LostCodeSubject = "فراموشی کد رهگیری";

    /// <summary>Orders placed with a mobile number; shown to staff on tickets so they can find a customer's lost tracking code.</summary>
    public async Task<IReadOnlyList<MobileOrderRow>> OrdersForMobileAsync(string mobile, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Orders.AsNoTracking().Where(o => o.Mobile == mobile)
            .OrderByDescending(o => o.Id).Take(20)
            .Select(o => new MobileOrderRow(o.Id, o.OrderNumber, o.TrackingCode, o.CustomerName, o.Status, o.TotalAmount, o.CreatedAt,
                o.Items.Select(i => i.Title).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public async Task<Result<Guid>> CreateAsync(NewTicketInput input, CancellationToken ct = default)
    {
        if (Validate(input) is { } error) return Result<Guid>.Fail(error);
        var mobile = Persian.NormalizeMobile(input.Mobile);
        if (mobile is null) return Result<Guid>.Fail("شماره موبایل معتبر نیست (مثال: ۰۹۱۲۱۲۳۴۵۶۷).");
        var code = Persian.NormalizeDigits(input.Code ?? "").Trim();
        if (!input.LostCode && code.Length == 0) return Result<Guid>.Fail("کد رهگیری یا شماره سفارش را وارد کنید.");

        await using var db = await dbFactory.CreateAsync(ct);
        int? orderId = null;
        if (!input.LostCode)
        {
            orderId = await db.Orders.Where(o => (o.TrackingCode == code || o.OrderNumber == code) && o.Mobile == mobile)
                .Select(o => (int?)o.Id).FirstOrDefaultAsync(ct);
            if (orderId is null)
                return Result<Guid>.Fail("سفارشی با این کد رهگیری / شماره سفارش و شماره موبایل یافت نشد. شماره موبایلی را وارد کنید که هنگام خرید ثبت کرده‌اید، یا گزینه «کد رهگیری را فراموش کرده‌ام» را انتخاب کنید.");
        }

        var openCount = await db.Tickets.CountAsync(t => t.Mobile == mobile && t.Status != TicketStatus.Closed, ct);
        if (openCount >= 5) return Result<Guid>.Fail("شما چند تیکت باز دارید. لطفاً از همان تیکت‌ها پیگیری کنید.");

        var now = clock.UtcNow;
        var ticket = new Ticket
        {
            TicketNumber = await NewNumberAsync(db, ct),
            OrderId = orderId,
            FullName = Persian.NormalizeText(input.FullName),
            Mobile = mobile,
            Subject = input.LostCode ? LostCodeSubject : Persian.NormalizeText(input.Subject),
            Status = TicketStatus.Open,
            CreatedAt = now,
            LastActivityAt = now,
            Messages = [new TicketMessage { Body = input.Message.Trim(), IsFromStaff = false, AuthorName = Persian.NormalizeText(input.FullName), CreatedAt = now }],
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Ticket {Number} created for order {OrderId}", ticket.TicketNumber, orderId);
        return Result<Guid>.Ok(ticket.PublicId);
    }

    public async Task<Guid?> FindAsync(string ticketNumber, string mobile, CancellationToken ct = default)
    {
        var m = Persian.NormalizeMobile(mobile);
        var number = Persian.NormalizeDigits(ticketNumber ?? "").Trim().ToUpperInvariant();
        if (m is null || number.Length is < 4 or > 20) return null;
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Tickets.Where(t => t.TicketNumber == number && t.Mobile == m).Select(t => (Guid?)t.PublicId).FirstOrDefaultAsync(ct);
    }

    public async Task<Ticket?> GetPublicAsync(Guid publicId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Tickets.AsNoTracking()
            .Include(t => t.Messages.OrderBy(m => m.Id))
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.PublicId == publicId, ct);
    }

    public async Task<Result> CustomerReplyAsync(Guid publicId, TicketReplyInput input, CancellationToken ct = default)
    {
        if (Validate(input) is { } error) return Result.Fail(error);
        await using var db = await dbFactory.CreateAsync(ct);
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.PublicId == publicId, ct);
        if (ticket is null) return Result.Fail("تیکت یافت نشد.");
        if (ticket.Status == TicketStatus.Closed) return Result.Fail("این تیکت بسته شده است. برای موضوع جدید، تیکت جدید ثبت کنید.");
        var now = clock.UtcNow;
        db.TicketMessages.Add(new TicketMessage { TicketId = ticket.Id, Body = input.Message.Trim(), IsFromStaff = false, AuthorName = ticket.FullName, CreatedAt = now });
        ticket.Status = TicketStatus.Open;
        ticket.LastActivityAt = now;
        ticket.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ---------- Admin ----------

    public async Task<PagedResult<TicketRow>> SearchAsync(TicketStatus? status, string? query, int page, int pageSize = 20, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = db.Tickets.AsNoTracking().AsQueryable();
        if (status is { } s) q = q.Where(t => t.Status == s);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = Persian.NormalizeDigits(query.Trim());
            q = q.Where(t => t.TicketNumber.Contains(term) || t.FullName.Contains(term) || t.Mobile.Contains(term) || t.Subject.Contains(term)
                             || (t.Order != null && (t.Order.OrderNumber.Contains(term) || (t.Order.TrackingCode != null && t.Order.TrackingCode.Contains(term)))));
        }
        var total = await q.CountAsync(ct);
        page = Math.Max(1, page);
        var rows = await q.OrderBy(t => t.Status).ThenByDescending(t => t.LastActivityAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TicketRow(t.Id, t.TicketNumber, t.Subject, t.FullName, t.Mobile, t.Order != null ? t.Order.OrderNumber : null,
                t.Status, t.CreatedAt, t.LastActivityAt, t.Messages.Count))
            .ToListAsync(ct);
        return new PagedResult<TicketRow>(rows, total, page, pageSize);
    }

    public async Task<int> CountOpenAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Tickets.CountAsync(t => t.Status == TicketStatus.Open, ct);
    }

    public async Task<Ticket?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Tickets.AsNoTracking()
            .Include(t => t.Messages.OrderBy(m => m.Id))
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Result> StaffReplyAsync(int id, string? message, AdminActor actor, bool close, CancellationToken ct = default)
    {
        var body = message?.Trim() ?? "";
        if (body.Length is < 2 or > 4000) return Result.Fail("متن پاسخ باید بین ۲ تا ۴۰۰۰ حرف باشد.");
        await using var db = await dbFactory.CreateAsync(ct);
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return Result.Fail("تیکت یافت نشد.");
        var now = clock.UtcNow;
        db.TicketMessages.Add(new TicketMessage { TicketId = ticket.Id, Body = body, IsFromStaff = true, AuthorName = actor.UserName, CreatedAt = now });
        ticket.Status = close ? TicketStatus.Closed : TicketStatus.Answered;
        ticket.LastActivityAt = now;
        ticket.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actor, "ReplyTicket", nameof(Ticket), ticket.TicketNumber, close ? "پاسخ و بستن" : "پاسخ", ct);
        return Result.Ok();
    }

    public async Task<Result> SetStatusAsync(int id, TicketStatus status, AdminActor actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return Result.Fail("تیکت یافت نشد.");
        ticket.Status = status;
        ticket.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actor, "TicketStatus", nameof(Ticket), ticket.TicketNumber, DisplayNames.Of(status), ct);
        return Result.Ok();
    }

    private static string? Validate(object input)
    {
        var errors = new List<ValidationResult>();
        return Validator.TryValidateObject(input, new ValidationContext(input), errors, true) ? null : errors[0].ErrorMessage;
    }

    /// <summary>Ticket numbers look like T482913: easy to read over the phone.</summary>
    private static async Task<string> NewNumberAsync(IAppDbContext db, CancellationToken ct)
    {
        for (var i = 0; i < 10; i++)
        {
            var n = $"T{RandomNumberGenerator.GetInt32(100000, 999999)}";
            if (!await db.Tickets.AnyAsync(t => t.TicketNumber == n, ct)) return n;
        }
        return $"T{RandomNumberGenerator.GetInt32(10_000_000, 99_999_999)}";
    }
}
