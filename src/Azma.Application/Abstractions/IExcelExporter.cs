namespace Azma.Application.Abstractions;

public sealed record ExcelColumn<T>(string Header, Func<T, object?> Value);

public interface IExcelExporter
{
    byte[] Export<T>(string sheetName, IReadOnlyList<ExcelColumn<T>> columns, IEnumerable<T> rows);
}
