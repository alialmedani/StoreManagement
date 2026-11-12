namespace StoreManagement.Common;

/// <summary>
/// Raw DevExtreme DataGrid/DataSource remote-operations query params, bound as-is from the query string.
/// When any DX field is present the list endpoint switches to the DevExtreme load path; otherwise it uses
/// the legacy ABP paging (SkipCount/MaxResultCount/Sorting/Filter). The DX `filter` param reuses the
/// existing <c>Filter</c> property (it carries the DX filter JSON in DX mode, a plain search string otherwise).
/// </summary>
public interface IDevExtremeLoadOptions
{
    string? Filter { get; }
    string? Sort { get; }
    string? Group { get; }
    string? Select { get; }
    string? TotalSummary { get; }
    string? GroupSummary { get; }
    int? Skip { get; }
    int? Take { get; }
    bool? RequireTotalCount { get; }
    bool? RequireGroupCount { get; }
}
