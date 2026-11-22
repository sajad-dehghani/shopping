using Azma.Application.Abstractions;
using Azma.Domain.Auditing;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace Azma.Infrastructure.Services;

public sealed class IranSiteClock(TimeProvider time) : ISiteClock
{
    private static readonly TimeZoneInfo Zone = FindZone();

    public DateTime UtcNow => time.GetUtcNow().UtcDateTime;
    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, Zone);
    public DateOnly Today => DateOnly.FromDateTime(LocalNow);

    public DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public DateTime StartOfLocalDayUtc(DateOnly date)
        => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Zone);

    private static TimeZoneInfo FindZone()
    {
        foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Iran", TimeSpan.FromHours(3.5), "Iran", "Iran");
    }
}

public sealed class AuditLogger(IAppDbContextFactory dbFactory, ISiteClock clock, ILogger<AuditLogger> logger) : IAuditLogger
{
    public async Task LogAsync(AdminActor actor, string action, string entityType, object? entityId, string? details = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Audit: {User} {Action} {EntityType}#{EntityId}", actor.UserName, action, entityType, entityId);
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        db.AdminAuditLogs.Add(new AdminAuditLog
        {
            UserId = actor.UserId,
            UserName = actor.UserName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId?.ToString(),
            Details = details is { Length: > 4000 } ? details[..4000] : details,
            IpAddress = actor.IpAddress,
            CreatedAt = clock.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ClosedXmlExcelExporter : IExcelExporter
{
    public byte[] Export<T>(string sheetName, IReadOnlyList<ExcelColumn<T>> columns, IEnumerable<T> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName.Length > 31 ? sheetName[..31] : sheetName);
        sheet.RightToLeft = true;
        for (var c = 0; c < columns.Count; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF7");
        }
        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(r, c + 1);
                switch (columns[c].Value(row))
                {
                    case null: break;
                    case long l: cell.Value = l; cell.Style.NumberFormat.Format = "#,##0"; break;
                    case int i: cell.Value = i; break;
                    // Text values are written as text so spreadsheet formulas are never interpreted (CSV/formula injection).
                    case var v: cell.SetValue(v.ToString()); break;
                }
            }
            r++;
        }
        sheet.Row(1).SetAutoFilter();
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(1, Math.Min(r, 200), 8, 60);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
