using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Repositories.FinancialGoals;
using Moq;

namespace CommonTestUtilities.Repositories
{
    public class FinancialGoalReadOnlyRepositoryBuilder
    {
        private readonly Mock<IFinancialGoalReadOnlyRepository> _repository = new();

        public FinancialGoalReadOnlyRepositoryBuilder ()
        {
            _repository
                .Setup(r => r.GetByPeriodAsync(It.IsAny<long>() , It.IsAny<int>() , It.IsAny<int>() , It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FinancialGoal>());
        }

        public FinancialGoalReadOnlyRepositoryBuilder Exists ( User user , long categoryId , int month , int year )
        {
            _repository
                .Setup(r => r.ExistsAsync(user.Id , categoryId , month , year , It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            return this;
        }

        public FinancialGoalReadOnlyRepositoryBuilder GetById ( User user , FinancialGoal? goal )
        {
            if ( goal is not null ) {
                _repository
                    .Setup(r => r.GetByIdAsync(user.Id , goal.Id , It.IsAny<CancellationToken>()))
                    .ReturnsAsync(goal);
            }

            return this;
        }

        public FinancialGoalReadOnlyRepositoryBuilder GetByPeriod ( User user , int month , int year , List<FinancialGoal> goals )
        {
            _repository
                .Setup(r => r.GetByPeriodAsync(user.Id , month , year , It.IsAny<CancellationToken>()))
                .ReturnsAsync(goals);

            return this;
        }

        public IFinancialGoalReadOnlyRepository Build () => _repository.Object;
    }

    public class FinancialGoalUpdateOnlyRepositoryBuilder
    {
        private readonly Mock<IFinancialGoalUpdateOnlyRepository> _repository = new();

        public FinancialGoalUpdateOnlyRepositoryBuilder GetById ( User user , FinancialGoal? goal )
        {
            if ( goal is not null ) {
                _repository
                    .Setup(r => r.GetByIdAsync(user.Id , goal.Id , It.IsAny<CancellationToken>()))
                    .ReturnsAsync(goal);
            }

            return this;
        }

        public IFinancialGoalUpdateOnlyRepository Build () => _repository.Object;
    }

    public class FinancialGoalWriteOnlyRepositoryBuilder
    {
        private readonly Mock<IFinancialGoalWriteOnlyRepository> _repository = new();

        public Mock<IFinancialGoalWriteOnlyRepository> GetMock () => _repository;

        public IFinancialGoalWriteOnlyRepository Build () => _repository.Object;
    }

    public class CategoriesReadOnlyRepositoryBuilder
    {
        private readonly Mock<ICategoriesReadOnlyRepository> _repository = new();

        public CategoriesReadOnlyRepositoryBuilder GetById ( User user , Category? category )
        {
            if ( category is not null ) {
                _repository
                    .Setup(r => r.GetByIdAsync(user , category.Id , It.IsAny<CancellationToken>()))
                    .ReturnsAsync(category);
            }

            return this;
        }

        public ICategoriesReadOnlyRepository Build () => _repository.Object;
    }
}
