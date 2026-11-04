using System.Text.RegularExpressions;

namespace MiCash.Application.UseCases.Users
{
    /// <summary>
    /// Brazilian mobile number: DDD (two digits 1-9) + 9 digits starting with 9. Same rule as the web and the
    /// mobile app (isValidMobilePhone); any mask is accepted and only the digits are stored.
    /// </summary>
    public static partial class PhoneRules
    {
        [GeneratedRegex(@"^[1-9][1-9]9\d{8}$")]
        private static partial Regex MobilePattern ();

        /// <summary>Only the digits; "+55" in front is dropped.</summary>
        public static string Digits ( string phone )
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length > 11 && digits.StartsWith("55") ? digits[2..] : digits;
        }

        public static bool IsValidMobile ( string phone ) => MobilePattern().IsMatch(Digits(phone));
    }
}
