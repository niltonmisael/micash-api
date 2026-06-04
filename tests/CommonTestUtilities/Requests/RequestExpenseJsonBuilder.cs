using Bogus;
using MiCash.Communication.Enums;
using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTestUtilities.Requests
{
    public class RequestExpenseJsonBuilder
    {
        public static RequestExpenseJson Build ()
        {
            return new Faker<RequestExpenseJson>()
                .RuleFor(r => r.Title , faker => faker.Commerce.ProductName())
                .RuleFor(r => r.Description , faker => faker.Commerce.ProductDescription())
                .RuleFor(r => r.ExpenseDate , faker => faker.Date.Past())
                .RuleFor(r => r.PaymentType , faker => faker.PickRandom<PaymentType>())
                .RuleFor(r => r.Amount , faker => faker.Random.Decimal(min: 1 , max: 1000));
        }

    }
}
