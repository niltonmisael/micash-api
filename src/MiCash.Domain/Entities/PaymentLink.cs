namespace MiCash.Domain.Entities
{
    public class PaymentLink
    {
        /// <summary>
        /// Tabela utilizada para correlacionar os Payment Links criados no Pagar.me
        /// com os usuários e planos do MiCash.
        ///
        /// Seu objetivo principal é fornecer rastreabilidade e auditoria do fluxo
        /// de pagamento, permitindo identificar o usuário responsável por um
        /// determinado Payment Link quando eventos forem recebidos via webhook.
        ///
        /// Esta entidade não representa uma regra de negócio principal do domínio,
        /// sendo utilizada apenas como apoio à integração com o gateway de pagamento.
        /// </summary>
        public long Id { get; set; }

        public long UserId { get; set; }

        public long PlanId { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public string PagarmePaymentLinkId { get; set; } = string.Empty;

        public string CheckoutUrl { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
