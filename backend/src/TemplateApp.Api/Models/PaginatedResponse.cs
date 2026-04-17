namespace TemplateApp.Api.Models;

/// <summary>
/// Represents a paginated response.
/// </summary>
/// <typeparam name="T">Type of items in the list.</typeparam>
/// <param name="List">List of items for the current page.</param>
/// <param name="Count">Total count of items.</param>
/// <param name="PageNumber">Current page number.</param>
/// <param name="PageSize">Number of items per page.</param>
public record PaginatedResponse<T>(
  IEnumerable<T>? List,
  int Count,
  int PageNumber,
  int PageSize
)
{
  public int TotalPages => (int)Math.Ceiling((double)Count / PageSize);
  public bool HasPreviousPage => PageNumber > 1;
  public bool HasNextPage => PageNumber < TotalPages;
}
