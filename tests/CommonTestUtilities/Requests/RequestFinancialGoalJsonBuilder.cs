using Bogus;
using MiCash.Communication.Requests.FinancialGoals;

namespace CommonTestUtilities.Requests
{
    public class RequestFinancialGoalJsonBuilder
    {
        public static RequestRegisterFinancialGoal BuildRegister ( long categoryId = 1 )
        {
            return new Faker<RequestRegisterFinancialGoal>()
                .RuleFor(r => r.CategoryId , _ => categoryId)
                .RuleFor(r => r.TargetAmount , faker => faker.Random.Decimal(min: 100 , max: 5000))
                .RuleFor(r => r.Month , faker => faker.Random.Int(1 , 12))
                .RuleFor(r => r.Year , _ => DateTime.UtcNow.Year + 1);
        }

        public static RequestUpdateFinancialGoalJson BuildUpdate ()
        {
            return new Faker<RequestUpdateFinancialGoalJson>()
                .RuleFor(r => r.TargetAmount , faker => faker.Random.Decimal(min: 100 , max: 5000));
        }

        public static RequestCopyFinancialGoalsJson BuildCopy ()
        {
            var year = DateTime.UtcNow.Year;

            return new RequestCopyFinancialGoalsJson
            {
                SourceMonth = 1 ,
                SourceYear = year ,
                TargetMonth = 2 ,
                TargetYear = year
            };
        }
    }
}
