using Bogus;
using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTestUtilities.Requests
{
    public class RequestLoginJsonBuilder
    {
        public static RequestLoginJson Build ()
        {
            return new Faker<RequestLoginJson>()
                .RuleFor(user => user.Email , faker => faker.Internet.Email())
                .RuleFor(user => user.Password , faker => faker.Internet.Password(prefix: "!Aa1"));
        }
    }
}
