using Bogus;
using MiCash.Communication.Requests;
using System.Net.NetworkInformation;

namespace CommonTestUtilities.Requests
{
    public class RequestRegisterUserJsonBuilder
    {
        public static RequestRegisterUserJson Build() 
        {
            return new Faker<RequestRegisterUserJson>()
                .RuleFor(user => user.Name , faker => faker.Person.FirstName)
                .RuleFor(user => user.Email , ( faker , user ) => faker.Internet.Email(user.Email))
                .RuleFor(user => user.Password , faker => faker.Internet.Password(prefix: "!Aa1"));
        }
    }
}
