using GameFlow.Application.Features.Search;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>Global arama (komut paleti): kullanıcı, görev, takım, proje, dosya.</summary>
public class SearchController(ISearchService searchService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SearchResultsDto>> Search(
        [FromQuery] SearchRequest request,
        CancellationToken cancellationToken)
        => Ok(await searchService.SearchAsync(request, cancellationToken));
}
