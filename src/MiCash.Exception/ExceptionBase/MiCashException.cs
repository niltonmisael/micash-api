using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Exception.ExceptionBase
{
    public abstract class MiCashException : SystemException
    {
        protected MiCashException (string message) : base (message) { }

        public abstract int StatusCode { get; }
        public abstract List<string> GetErrors ();

    }

    
}
