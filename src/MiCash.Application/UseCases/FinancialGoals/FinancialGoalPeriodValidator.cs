using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGoals
{
    /// <summary>
    /// Validates the month/year pair received as query parameters by the read endpoints.
    /// </summary>
    public static class FinancialGoalPeriodValidator
    {
        public const int MIN_YEAR = 2000;
        public const int MAX_YEAR = 2100;

        public static void Validate ( int month , int year )
        {
            var errors = new List<string>();

            if ( month < 1 || month > 12 ) {
                errors.Add(ResourceErrorMessages.MONTH_INVALID_RANGE);
            }

            if ( year < MIN_YEAR || year > MAX_YEAR ) {
                errors.Add(ResourceErrorMessages.YEAR_INVALID);
            }

            if ( errors.Count > 0 ) {
                throw new ErrorOnValidationException(errors);
            }
        }
    }
}
