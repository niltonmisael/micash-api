namespace MiCash.Communication.Requests.PushDevices
{
    public class RequestRegisterPushDeviceJson
    {
        /// <summary>ExponentPushToken[...] returned by getExpoPushTokenAsync.</summary>
        public string ExpoPushToken { get; set; } = string.Empty;

        /// <summary>"android" or "ios".</summary>
        public string Platform { get; set; } = string.Empty;

        /// <summary>Optional stable id of the install; lets the API retire older tokens of the same device.</summary>
        public string? DeviceId { get; set; }

        public string? DeviceName { get; set; }
    }

    public class RequestDeactivatePushDeviceJson
    {
        public string ExpoPushToken { get; set; } = string.Empty;
    }
}
