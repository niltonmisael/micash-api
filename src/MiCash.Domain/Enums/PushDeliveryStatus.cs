namespace MiCash.Domain.Enums
{
    /// <summary>
    /// Lifecycle of one notification on one device. A ticket from Expo is NOT a delivery
    /// confirmation: only the receipt moves a delivery to <see cref="Delivered"/>.
    /// </summary>
    public enum PushDeliveryStatus
    {
        /// <summary>Waiting to be sent (also after a temporary failure, until NextAttemptAt).</summary>
        Pending = 0,

        /// <summary>Claimed by a job and handed to Expo; never sent again from this state.</summary>
        Sending = 1,

        /// <summary>Expo accepted it and returned a ticket; waiting for the receipt.</summary>
        SentToExpo = 2,

        /// <summary>Receipt "ok": Expo handed it to FCM/APNs.</summary>
        Delivered = 3,

        /// <summary>Definitive error (ticket/receipt error, rejected request or attempts exhausted).</summary>
        Failed = 4,

        /// <summary>Outcome unknown (timeout after sending, interrupted job or no receipt within 24 h). Not resent, to avoid duplicates.</summary>
        Unconfirmed = 5
    }
}
