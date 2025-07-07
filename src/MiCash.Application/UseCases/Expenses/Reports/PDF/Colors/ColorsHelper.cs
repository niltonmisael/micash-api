using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MigraDoc.DocumentObjectModel;

namespace MiCash.Application.UseCases.Expenses.Reports.PDF.Colors
{
    public static class ColorsHelper
    {
        public static readonly Color RED_DARK = Color.Parse("#DF340C");
        public static readonly Color RED_LIGHT = Color.Parse("#F5C2B6");
        public static readonly Color GREEN_DARK = Color.Parse("#D7E5E5");
        public static readonly Color GREEN_LIGHT = Color.Parse("#F5F9F8");
        public static readonly Color BLACK = Color.Parse("#000000");
        public static readonly Color WHITE = Color.Parse("#FFFFFF");
    }
}
