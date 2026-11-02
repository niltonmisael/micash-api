using FluentValidation;
using MiCash.Communication.Requests.PushDevices;
using MiCash.Domain.Enums;
using MiCash.Exception;
using System.Text.RegularExpressions;

namespace MiCash.Application.UseCases.PushDevices
{
    internal static partial class PushDeviceRules
    {
        public const int TOKEN_MAX_LENGTH = 255;
        public const int DEVICE_ID_MAX_LENGTH = 200;
        public const int DEVICE_NAME_MAX_LENGTH = 100;

        public static bool IsExpoPushToken ( string? token ) =>
            !string.IsNullOrEmpty(token) && token.Length <= TOKEN_MAX_LENGTH && TokenRegex().IsMatch(token);

        public static DevicePlatform? ParsePlatform ( string? platform ) => platform?.Trim().ToLowerInvariant() switch
        {
            "android" => DevicePlatform.Android,
            "ios" => DevicePlatform.Ios,
            _ => null
        };

        public static string ToJson ( DevicePlatform platform ) =>
            platform == DevicePlatform.Ios ? "ios" : "android";

        public static void Token<T> ( IRuleBuilderInitial<T , string> rule ) =>
            rule.Must(IsExpoPushToken).WithMessage(ResourceErrorMessages.PUSH_TOKEN_INVALID);

        [GeneratedRegex(@"^(ExponentPushToken|ExpoPushToken)\[[A-Za-z0-9_\-]+\]$")]
        private static partial Regex TokenRegex ();
    }

    public class ValidatorRegisterPushDevice : AbstractValidator<RequestRegisterPushDeviceJson>
    {
        public ValidatorRegisterPushDevice ()
        {
            PushDeviceRules.Token(RuleFor(x => x.ExpoPushToken));

            RuleFor(x => x.Platform)
                .Must(platform => PushDeviceRules.ParsePlatform(platform) is not null)
                .WithMessage(ResourceErrorMessages.PUSH_PLATFORM_INVALID);

            RuleFor(x => x.DeviceId)
                .MaximumLength(PushDeviceRules.DEVICE_ID_MAX_LENGTH)
                .WithMessage(ResourceErrorMessages.PUSH_DEVICE_ID_TOO_LONG);

            RuleFor(x => x.DeviceName)
                .MaximumLength(PushDeviceRules.DEVICE_NAME_MAX_LENGTH)
                .WithMessage(ResourceErrorMessages.PUSH_DEVICE_NAME_TOO_LONG);
        }
    }

    public class ValidatorDeactivatePushDevice : AbstractValidator<RequestDeactivatePushDeviceJson>
    {
        public ValidatorDeactivatePushDevice ()
        {
            PushDeviceRules.Token(RuleFor(x => x.ExpoPushToken));
        }
    }
}
