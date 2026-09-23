namespace Application.Common.Models;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Size, int TotalCount)
{
    public int TotalPages => Size == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)Size);
}
