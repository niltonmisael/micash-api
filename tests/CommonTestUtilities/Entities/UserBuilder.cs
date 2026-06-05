using Bogus;
using CommonTestUtilities.Cryptography;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;

namespace CommonTestUtilities.Entities
{
    public class UserBuilder
    {
        public static User Build ()
        {
            var passwordEncripter = new PasswordEncrypterBuilder().Build();

            var user = new Faker<User>()
                .RuleFor(u => u.Id , _ => 1)
                .RuleFor(u => u.Name , faker => faker.Person.FirstName)
                .RuleFor(u => u.Email , ( faker , user ) => faker.Internet.Email(user.Name))
                .RuleFor(u => u.PasswordHash , ( _ , user ) => passwordEncripter.Encrypt(user.PasswordHash))
                .RuleFor(u => u.UserIdentifier , _ => Guid.NewGuid());

            return user;
        }
    }
}
