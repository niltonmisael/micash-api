using MiCash.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Domain.Extensions
{
    public static class PaymentTypeExtensions
    {
        public static string PaymentTypeToString(this PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => "Dinheiro",
                PaymentType.CreditCard => "Cartão de Crédito",
                PaymentType.DebitCard => "Cartão de Débito",
                PaymentType.EletronicTransfer => "Transferencia Bancária",
                _ => string.Empty

                // Implementar o Resource
            };
        }
    }
}
