using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Communication.Responses
{
    public class ResponseUserProfileJson
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        /// <summary>Digits only (DDD + number, e.g. "11987654321"); empty when the account has none.</summary>
        public string Phone { get; set; } = string.Empty;
    }
}
