namespace MiCash.Infrastructure.Services.Payments.Requests
{
    public class CreateCustomerRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Type { get; set; } = "individual";
    }
}
