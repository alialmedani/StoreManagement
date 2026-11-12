using Volo.Abp.Application.Dtos;

namespace StoreManagement.Common;

/// <summary>
/// Base paged/sorted request DTO. Additive DevExtreme (DX) remote-operations support: when any DX-only
/// field (<see cref="Sort"/>, <see cref="Group"/>, <see cref="Select"/>, <see cref="TotalSummary"/>,
/// <see cref="GroupSummary"/>, <see cref="Skip"/>, <see cref="Take"/>, <see cref="RequireTotalCount"/>,
/// <see cref="RequireGroupCount"/>) is present, the list endpoint switches to DX remote-operations mode.
/// </summary>
/// <remarks>
/// <see cref="Filter"/> carries two meanings depending on mode: in legacy mode it is a plain free-text
/// search string; in DX mode (see <see cref="IDevExtremeLoadOptions"/>) it is the DX filter JSON. DX's own
/// `filter` query param collides with this existing property name, so there is no separate DX-filter
/// property - callers must not rely on sending both meanings at once.
/// </remarks>
public class StoreManagementPagedAndSortedResultRequestDto : PagedAndSortedResultRequestDto, IDevExtremeLoadOptions
{
    /// <summary>
    /// Plain free-text search string in legacy mode, or the DevExtreme filter JSON in DX mode.
    /// </summary>
    public string? Filter { get; set; }

    public string? Sort { get; set; }

    public string? Group { get; set; }

    public string? Select { get; set; }

    public string? TotalSummary { get; set; }

    public string? GroupSummary { get; set; }

    public int? Skip { get; set; }

    public int? Take { get; set; }

    public bool? RequireTotalCount { get; set; }

    public bool? RequireGroupCount { get; set; }
}
