using Bogus;
using MiCash.Domain.Entities;

namespace CommonTestUtilities.Entities
{
    public class FinancialGoalBuilder
    {
        public static List<FinancialGoal> Collection ( User user , uint count = 2 , int? month = null , int? year = null )
        {
            if ( count == 0 )
                count = 1;

            var list = new List<FinancialGoal>();

            for ( var i = 1; i <= count; i++ ) {
                var category = CategoryBuilder.Build(user , id: i);
                var goal = Build(user , category , month , year);
                goal.Id = i;

                list.Add(goal);
            }

            return list;
        }

        public static FinancialGoal Build ( User user , Category? category = null , int? month = null , int? year = null )
        {
            category ??= CategoryBuilder.Build(user);

            return new Faker<FinancialGoal>()
                .RuleFor(g => g.Id , _ => 1)
                .RuleFor(g => g.UserId , _ => user.Id)
                .RuleFor(g => g.CategoryId , _ => category.Id)
                .RuleFor(g => g.Category , _ => category)
                .RuleFor(g => g.TargetAmount , faker => faker.Random.Decimal(min: 100 , max: 5000))
                .RuleFor(g => g.Month , faker => month ?? faker.Random.Int(1 , 12))
                .RuleFor(g => g.Year , _ => year ?? DateTime.UtcNow.Year);
        }
    }
}
