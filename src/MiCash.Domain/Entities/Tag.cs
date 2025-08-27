using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Domain.Entities
{
    public class Tag
    {
        public long Id { get; set; }
        public Enums.Tag Value { get; set; }
        public long ExpenseId { get; set; }
        public Expense Expense { get; set; } = default!;
    }
}
