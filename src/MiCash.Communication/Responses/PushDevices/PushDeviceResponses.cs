namespace MiCash.Communication.Responses.PushDevices
{
    public class ResponseRegisteredPushDeviceJson
    {
        public long Id { get; set; }
    }

    public class ResponsePushDevicesJson
    {
        public List<ResponsePushDeviceJson> Devices { get; set; } = [];
    }

    public class ResponsePushDeviceJson
    {
        public long Id { get; set; }

        /// <summary>"android" or "ios".</summary>
        public string Platform { get; set; } = string.Empty;

        public string? DeviceName { get; set; }

        /// <summary>Last characters of the token, to tell devices apart (the full token is never returned).</summary>
        public string TokenSuffix { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastUsedAt { get; set; }
    }

    public class ResponseTestPushNotificationJson
    {
        public long NotificationId { get; set; }

        /// <summary>Active devices the test was queued for.</summary>
        public int Devices { get; set; }
    }
}
