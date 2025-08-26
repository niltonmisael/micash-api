using Bogus;
using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTestUtilities.Requests
{
    public class RequestChangePasswordJsonBuilder
    {
        public static RequestChangePasswordJson Build ()
        {
            return new Faker<RequestChangePasswordJson>()
                .RuleFor(user => user.Password , faker => faker.Internet.Password())
                .RuleFor(user => user.NewPassword , faker => faker.Internet.Password(prefix: "!Aa1"));
        }
    }
}
