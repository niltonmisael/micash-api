namespace MiCash.Communication.Requests.FinancialGoals
{
    public class RequestCopyFinancialGoalsJson
    {
        public int SourceMonth { get; set; }

        public int SourceYear { get; set; }

        public int TargetMonth { get; set; }

        public int TargetYear { get; set; }
    }
}
