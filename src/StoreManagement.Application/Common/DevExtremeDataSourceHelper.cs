using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.Helpers;
using Volo.Abp.Application.Dtos;

namespace StoreManagement.Common;

/// <summary>
/// Bridges DevExtreme remote-operations load options onto an <see cref="IQueryable{T}"/> and returns the
/// result inside ABP's <see cref="PagedResultDto{T}"/> envelope so the existing mobile contract is untouched.
/// Only engage this path when <see cref="HasLoadOptions"/> is true.
/// </summary>
public static class DevExtremeDataSourceHelper
{
    private sealed class StoreManagementDxLoadOptions : DataSourceLoadOptionsBase
    {
    }

    /// <summary>
    /// True when the caller sent at least one DevExtreme remote-operations param.
    /// Note: the DX `filter` param reuses the ABP Filter property, so Filter alone does NOT trigger DX mode
    /// (that keeps the legacy plain-text filter behaviour intact); DX mode is detected via the DX-only params.
    /// </summary>
    public static bool HasLoadOptions(IDevExtremeLoadOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Sort)
            || !string.IsNullOrWhiteSpace(options.Group)
            || !string.IsNullOrWhiteSpace(options.Select)
            || !string.IsNullOrWhiteSpace(options.TotalSummary)
            || !string.IsNullOrWhiteSpace(options.GroupSummary)
            || options.Skip.HasValue
            || options.Take.HasValue
            || options.RequireTotalCount.HasValue
            || options.RequireGroupCount.HasValue;
    }

    public static async Task<PagedResultDto<T>> LoadAsync<T>(
        IQueryable<T> source,
        IDevExtremeLoadOptions options,
        CancellationToken cancellationToken = default)
    {
        var loadOptions = new StoreManagementDxLoadOptions();

        DataSourceLoadOptionsParser.Parse(loadOptions, key => GetRawValue(options, key));

        // The response contract is a flat PagedResultDto<T>. DevExtreme grouping would make
        // DataSourceLoader return Group wrapper objects (not T) in result.data, which would break the
        // Cast<T> below. We accept the `group`/`groupSummary` params (so a grid can send them) but
        // deliberately do not reshape the response into groups: clear them to keep output flat.
        loadOptions.Group = null;
        loadOptions.GroupSummary = null;
        loadOptions.RequireGroupCount = false;

        // Always ask for the total count so the PagedResultDto envelope is populated.
        loadOptions.RequireTotalCount = true;

        var result = await DataSourceLoader.LoadAsync(source, loadOptions, cancellationToken);

        var items = ((IEnumerable)result.data).Cast<T>().ToList();

        var totalCount = result.totalCount >= 0 ? result.totalCount : items.Count;

        return new PagedResultDto<T>(totalCount, items);
    }

    private static string GetRawValue(IDevExtremeLoadOptions options, string key)
    {
        return key switch
        {
            "filter" => options.Filter ?? string.Empty,
            "sort" => options.Sort ?? string.Empty,
            "group" => options.Group ?? string.Empty,
            "select" => options.Select ?? string.Empty,
            "totalSummary" => options.TotalSummary ?? string.Empty,
            "groupSummary" => options.GroupSummary ?? string.Empty,
            "skip" => options.Skip?.ToString() ?? string.Empty,
            "take" => options.Take?.ToString() ?? string.Empty,
            "requireTotalCount" => options.RequireTotalCount?.ToString() ?? string.Empty,
            "requireGroupCount" => options.RequireGroupCount?.ToString() ?? string.Empty,
            _ => string.Empty
        };
    }
}
