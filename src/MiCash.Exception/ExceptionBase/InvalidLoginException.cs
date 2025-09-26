using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Exception.ExceptionBase
{
    public class InvalidLoginException : MiCashException
    {
        public InvalidLoginException () : base(ResourseErrorMessages.EMAIL_OR_PASSWORD_INVALID) { }

        public override int StatusCode => (int)HttpStatusCode.Unauthorized;

        public override List<string> GetErrors ()
        {
            return [Message];
        }
    }
}
