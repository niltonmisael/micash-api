namespace MiCash.Domain.Services.Pagination
{
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = [];

        public int TotalRecords { get; set; }
    }
}
