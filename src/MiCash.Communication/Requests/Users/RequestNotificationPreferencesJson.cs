namespace MiCash.Communication.Requests.Users
{
    /// <summary>PUT api/user/notification-preferences (also the shape of the GET answer).</summary>
    public class RequestNotificationPreferencesJson
    {
        /// <summary>Monthly financial summary by e-mail (sent on the 1st, about the month that ended).</summary>
        public bool MonthlySummaryEmail { get; set; }

        /// <summary>Push "Seu resumo do mês chegou", opening the reports.</summary>
        public bool MonthlySummaryPush { get; set; }

        /// <summary>News and offers by e-mail (marketing, opt-in).</summary>
        public bool NewsEmail { get; set; }
    }
}
