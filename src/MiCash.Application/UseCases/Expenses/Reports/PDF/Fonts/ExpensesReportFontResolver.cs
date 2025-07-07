using MigraDoc.DocumentObjectModel;
using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses.Reports.PDF.Fonts
{
    public class ExpensesReportFontResolver : IFontResolver
    {
        public byte[]? GetFont ( string faceName )
        {
            var stream = ReadFontFile ( faceName );

            if ( stream is null ) {
                stream = ReadFontFile(FontHelper.DEFAULT_FONT);
            }

            var lenght = (int)stream!.Length;
            
            var data = new byte[lenght];

            stream.ReadExactly(buffer: data , offset: 0 , count: lenght); // ou Read

            return data;
        }

        public FontResolverInfo? ResolveTypeface ( string familyName , bool bold , bool italic )
        {
            

            return new FontResolverInfo(familyName);
        }

        private Stream? ReadFontFile (string faceName)
        {
            var assembly = Assembly.GetExecutingAssembly ();

            return assembly.GetManifestResourceStream ($"MiCash.Application.UseCases.Expenses.Reports.PDF.Fonts.{faceName}.ttf");
        }
    }
}
