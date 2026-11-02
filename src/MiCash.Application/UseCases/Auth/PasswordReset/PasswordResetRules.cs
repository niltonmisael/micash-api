using FluentValidation;
using MiCash.Application.UseCases.Users;
using MiCash.Communication.Requests.PasswordReset;
using MiCash.Exception;
using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MiCash.Application.UseCases.Auth.PasswordReset
{
    /// <summary>Rules and secrets of the password reset flow.</summary>
    public static partial class PasswordResetRules
    {
        public const int CODE_LENGTH = 6;

        /// <summary>Wrong or right code checks per code; then the code is invalidated.</summary>
        public const int MAX_ATTEMPTS = 5;

        /// <summary>Requests (codes) per user per hour.</summary>
        public const int MAX_REQUESTS_PER_HOUR = 5;

        public static readonly TimeSpan CodeValidity = TimeSpan.FromMinutes(15);

        public static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);

        public static readonly TimeSpan ResetTokenValidity = TimeSpan.FromMinutes(10);

        /// <summary>Uniform 0..999999 from a CSPRNG, zero-padded ("004213").</summary>
        public static string NewCode () =>
            RandomNumberGenerator.GetInt32(0 , 1_000_000).ToString("D6" , CultureInfo.InvariantCulture);

        /// <summary>256 random bits, URL-safe.</summary>
        public static string NewResetToken () =>
            Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

        /// <summary>The token has 256 bits of entropy: a fast hash is enough and allows the lookup by hash.</summary>
        public static string HashResetToken ( string token ) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        public static bool IsCodeFormat ( string? code ) => code is not null && CodePattern().IsMatch(code);

        [GeneratedRegex(@"^\d{6}$")]
        private static partial Regex CodePattern ();
    }

    public class ForgotPasswordValidator : AbstractValidator<RequestForgotPasswordJson>
    {
        public ForgotPasswordValidator ()
        {
            RuleFor(r => r.Email)
                .NotEmpty()
                .WithMessage(ResourceErrorMessages.EMAIL_EMPTY)
                .EmailAddress()
                .When(r => string.IsNullOrWhiteSpace(r.Email) == false , ApplyConditionTo.CurrentValidator)
                .WithMessage(ResourceErrorMessages.EMAIL_INVALID);
        }
    }

    public class VerifyPasswordResetCodeValidator : AbstractValidator<RequestVerifyPasswordResetCodeJson>
    {
        public VerifyPasswordResetCodeValidator ()
        {
            RuleFor(r => r.Email)
                .NotEmpty()
                .WithMessage(ResourceErrorMessages.EMAIL_EMPTY)
                .EmailAddress()
                .When(r => string.IsNullOrWhiteSpace(r.Email) == false , ApplyConditionTo.CurrentValidator)
                .WithMessage(ResourceErrorMessages.EMAIL_INVALID);

            RuleFor(r => r.Code)
                .Must(PasswordResetRules.IsCodeFormat)
                .WithMessage(ResourceErrorMessages.PASSWORD_RESET_CODE_FORMAT);
        }
    }

    public class ResetPasswordValidator : AbstractValidator<RequestResetPasswordJson>
    {
        public ResetPasswordValidator ()
        {
            RuleFor(r => r.ResetToken)
                .NotEmpty()
                .WithMessage(ResourceErrorMessages.PASSWORD_RESET_TOKEN_INVALID);

            RuleFor(r => r.NewPassword).SetValidator(new PasswordValidator<RequestResetPasswordJson>());
        }
    }
}
