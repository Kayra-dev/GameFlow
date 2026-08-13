namespace GameFlow.Application.Common.Models;

/// <summary>Sayfalama parametreleri için temel istek modeli.</summary>
public class PagedRequest
{
    private const int MaxPageSize = 100;

    private int _pageSize = 20;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public int Skip => (Page - 1) * PageSize;
}
