using MiCash.Communication.Requests.Incomes;

namespace MiCash.Communication.Responses.Incomes
{
    public class ResponseIncomesJson
    {
        public List<ResponseIncomeShortJson> Incomes { get; set; } = [];
    }
}
