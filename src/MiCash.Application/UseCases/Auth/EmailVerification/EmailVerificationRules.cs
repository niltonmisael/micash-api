using FluentValidation;
using MiCash.Application.UseCases.Auth.PasswordReset;
using MiCash.Communication.Requests.EmailVerification;
using MiCash.Communication.Responses.EmailVerification;
using MiCash.Exception;

namespace MiCash.Application.UseCases.Auth.EmailVerification
{
    /// <summary>
    /// Rules of the e-mail confirmation. Same code format as the password reset (6 digits, BCrypt hash),
    /// same limits; only the validity of an unverified account is specific.
    /// </summary>
    public static class EmailVerificationRules
    {
        public const int CODE_LENGTH = PasswordResetRules.CODE_LENGTH;

        /// <summary>Right or wrong checks per code; then the code is invalidated.</summary>
        public const int MAX_ATTEMPTS = 5;

        /// <summary>Codes per user per hour (registration, resends and e-mail corrections together).</summary>
        public const int MAX_REQUESTS_PER_HOUR = 5;

        public static readonly TimeSpan CodeValidity = TimeSpan.FromMinutes(15);

        public static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);

        /// <summary>Accounts never confirmed are deleted after this period, which frees the e-mail.</summary>
        public static readonly TimeSpan UnverifiedAccountLifetime = TimeSpan.FromDays(7);

        public static string NewCode () => PasswordResetRules.NewCode();

        public static ResponseEmailVerificationCodeSentJson CodeSent ( string email ) => new()
        {
            Email = email ,
            ResendAvailableInSeconds = (int)ResendInterval.TotalSeconds ,
            CodeExpiresInMinutes = (int)CodeValidity.TotalMinutes ,
            CodeLength = CODE_LENGTH
        };
    }

    public class VerifyEmailValidator : AbstractValidator<RequestVerifyEmailJson>
    {
        public VerifyEmailValidator ()
        {
            RuleFor(r => r.Code)
                .Must(PasswordResetRules.IsCodeFormat)
                .WithMessage(ResourceErrorMessages.PASSWORD_RESET_CODE_FORMAT);
        }
    }

    public class EmailChangeValidator : AbstractValidator<RequestEmailChangeJson>
    {
        public EmailChangeValidator ()
        {
            RuleFor(r => r.NewEmail)
                .NotEmpty()
                .WithMessage(ResourceErrorMessages.EMAIL_EMPTY)
                .EmailAddress()
                .When(r => string.IsNullOrWhiteSpace(r.NewEmail) == false , ApplyConditionTo.CurrentValidator)
                .WithMessage(ResourceErrorMessages.EMAIL_INVALID);

            RuleFor(r => r.Password)
                .NotEmpty()
                .WithMessage(ResourceErrorMessages.PASSWORD_DIFFERENT_CURRENT_PASSWORD);
        }
    }

    public class ConfirmEmailChangeValidator : AbstractValidator<RequestConfirmEmailChangeJson>
    {
        public ConfirmEmailChangeValidator ()
        {
            RuleFor(r => r.Code)
                .Must(PasswordResetRules.IsCodeFormat)
                .WithMessage(ResourceErrorMessages.PASSWORD_RESET_CODE_FORMAT);
        }
    }

    public class ChangeVerificationEmailValidator : AbstractValidator<RequestChangeVerificationEmailJson>
    {
        public ChangeVerificationEmailValidator ()
        {
            RuleFor(r => r.Email)
                .NotEmpty()
                .WithMessage(ResourceErrorMessages.EMAIL_EMPTY)
                .EmailAddress()
                .When(r => string.IsNullOrWhiteSpace(r.Email) == false , ApplyConditionTo.CurrentValidator)
                .WithMessage(ResourceErrorMessages.EMAIL_INVALID);
        }
    }
}
