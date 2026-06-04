namespace MiCash.Communication.Responses.Categories
{
    public class ResponseCategoryJson
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
