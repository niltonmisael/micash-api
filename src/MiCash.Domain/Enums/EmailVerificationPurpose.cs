namespace MiCash.Domain.Enums
{
    /// <summary>Why a confirmation code was sent.</summary>
    public enum EmailVerificationPurpose
    {
        /// <summary>Confirms the address of a new account (<see cref="Entities.User.Email"/>).</summary>
        SignUp = 0,

        /// <summary>Confirms a new address for a verified account; the e-mail only changes after the code.</summary>
        EmailChange = 1
    }
}
