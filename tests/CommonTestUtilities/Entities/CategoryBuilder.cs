using Bogus;
using MiCash.Domain.Entities;

namespace CommonTestUtilities.Entities
{
    public class CategoryBuilder
    {
        public static Category Build ( User user , long id = 1 , bool isActive = true )
        {
            return new Faker<Category>()
                .RuleFor(c => c.Id , _ => id)
                .RuleFor(c => c.Name , faker => faker.Commerce.Department())
                .RuleFor(c => c.Color , faker => faker.Internet.Color())
                .RuleFor(c => c.Icon , _ => "home")
                .RuleFor(c => c.IsActive , _ => isActive)
                .RuleFor(c => c.UserId , _ => user.Id);
        }
    }
}
