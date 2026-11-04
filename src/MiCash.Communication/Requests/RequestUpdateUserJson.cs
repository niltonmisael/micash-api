using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Communication.Requests
{
    public class RequestUpdateUserJson
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// Brazilian mobile with DDD; any mask is accepted and only the digits are stored. Null (or omitted) keeps
        /// the current phone, so clients that only edit the name do not erase it.
        /// </summary>
        public string? Phone { get; set; }
    }
}
