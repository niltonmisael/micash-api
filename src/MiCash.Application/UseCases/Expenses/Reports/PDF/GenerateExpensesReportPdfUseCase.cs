
using DocumentFormat.OpenXml.Bibliography;
using MiCash.Application.UseCases.Expenses.Reports.PDF.Colors;
using MiCash.Application.UseCases.Expenses.Reports.PDF.Fonts;
using MiCash.Domain.Entities;
using MiCash.Domain.Reports;
using MiCash.Domain.Repositories.Expenses;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MiCash.Domain.Extensions;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Expenses.Reports.PDF
{
    public class GenerateExpensesReportPdfUseCase : IGenerateExpensesReportPdfUseCase
    {
        private const string CURRENCY_SYBOL = "R$";
        private const int HEIGHT_ROW_EXPENSE_TABLE = 25;

        private readonly IExpensesReadOnlyRepository _repository;
        private readonly ILoggedUser _loggedUser;

        public GenerateExpensesReportPdfUseCase (IExpensesReadOnlyRepository repository, ILoggedUser loggedUser)
        {
            _repository = repository;
            _loggedUser = loggedUser;

            GlobalFontSettings.FontResolver = new ExpensesReportFontResolver();
        }

        public async Task<byte[]> Execute (DateOnly month )
        {
            var loggedUser = await _loggedUser.Get();

            var expenses = await _repository.FilterByMonth( loggedUser ,month );

            if ( expenses.Count == 0 ) {
                return [];
            }

            var document = CreateDocument( loggedUser.Name ,month);
            var page = CreatePage(document);

            CreateHeaderWithProfileAndName(loggedUser.Name , page );

            var totalExpenses = expenses.Sum(expense => expense.Amount);

            CreateTotalSpentSection(page,month, totalExpenses);

            foreach ( var expense in expenses ) {

                var table = CreateExpenseTable(page);

                var row =table.AddRow();
                row.Height = HEIGHT_ROW_EXPENSE_TABLE;

                AddExpenseTitle(row.Cells[0], expense.Title );

                AddHeaderForAmount(row.Cells[3]);
                
                // Nova Linha
                row = table.AddRow();
                row.Height = HEIGHT_ROW_EXPENSE_TABLE;

                row.Cells[0].AddParagraph(expense.Date.ToString("D"));
                SetStyleBaseForExpenseInformation(row.Cells[0]);
                row.Cells[0].Format.LeftIndent = 20;

                row.Cells[1].AddParagraph(expense.Date.ToString("t"));
                SetStyleBaseForExpenseInformation(row.Cells[1]);

                row.Cells[2].AddParagraph(expense.PaymentType.PaymentTypeToString());
                SetStyleBaseForExpenseInformation(row.Cells[2]);

                AddAmountForExpense(row.Cells[3], expense.Amount);

                if ( string.IsNullOrWhiteSpace(expense.Description) == false ) {

                    var description = table.AddRow();
                    description.Height = HEIGHT_ROW_EXPENSE_TABLE;
                    description.Cells[0].AddParagraph( expense.Description );
                    description.Cells[0].Format.Font = new Font { Name = FontHelper.WORKSANS_REGULAR , Size = 10 , Color = ColorsHelper.BLACK };
                    description.Cells[0].Shading.Color = ColorsHelper.GREEN_LIGHT;
                    description.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                    description.Cells[0].MergeRight = 2;
                    description.Cells[0].Format.LeftIndent = 20;

                    row.Cells[3].MergeDown = 1;

                }


                AddWhiteSpace(table);
                
            }

            return RenderDocument(document);
        }

        private Document CreateDocument ( string author , DateOnly month )
        {
            var document = new Document ();

            document.Info.Title = $"{ResourceReportGenerationMessages.EXPENSE_FOR} {month:Y}";
            document.Info.Author = author;

            var style = document.Styles["Normal"];
            style!.Font.Name = FontHelper.RALEWAY_REGULAR;

            return document;
        }

        private Section CreatePage ( Document document )
        {
            var section = document.AddSection();
            section.PageSetup = document.DefaultPageSetup.Clone();

            section.PageSetup.PageFormat = PageFormat.A4;

            section.PageSetup.LeftMargin = 40;
            section.PageSetup.RightMargin = 40;
            section.PageSetup.TopMargin = 80;
            section.PageSetup.BottomMargin = 80;

            return section;

        }

        private void CreateHeaderWithProfileAndName (string name, Section page)
        {
            var table = page.AddTable();
            table.AddColumn();
            table.AddColumn("300"); // pixeis

            var row = table.AddRow();

            var assembly = Assembly.GetExecutingAssembly();
            var diretoryName = Path.GetDirectoryName(assembly.Location);
            var pathFile = Path.Combine(diretoryName! , "Logo" , "perfilmicash.png");


            row.Cells[0].AddImage(pathFile);

            row.Cells[1].AddParagraph($"Hey, {name}");
            row.Cells[1].Format.Font = new Font { Name = FontHelper.RALEWAY_BLACK , Size = 16 };
            row.Cells[1].VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
        }

        private void CreateTotalSpentSection(Section page, DateOnly month, decimal totalExpenses )
        {
            var paragraph = page.AddParagraph();
            paragraph.Format.SpaceBefore = "40";
            paragraph.Format.SpaceAfter = "40";

            var title = string.Format(ResourceReportGenerationMessages.TOTAL_SPENT_IN , month.ToString("Y"));

            paragraph.AddFormattedText(title , new Font { Name = FontHelper.RALEWAY_REGULAR , Size = 15 });
            paragraph.AddLineBreak();

            
            paragraph.AddFormattedText($"{CURRENCY_SYBOL} {totalExpenses:f2}" , new Font { Name = FontHelper.WORKSANS_BLACK , Size = 50 });
        }

        private Table CreateExpenseTable (Section page)
        {
            var table = page.AddTable();
            table.AddColumn("195").Format.Alignment = ParagraphAlignment.Left;
            table.AddColumn("80").Format.Alignment = ParagraphAlignment.Center;
            table.AddColumn("120").Format.Alignment = ParagraphAlignment.Center;
            table.AddColumn("120").Format.Alignment = ParagraphAlignment.Right;

            return table;
        }

        private void AddExpenseTitle (Cell row, string expenseTitle)
        {
            row.AddParagraph(expenseTitle);
            row.Format.Font = new Font { Name = FontHelper.RALEWAY_BLACK , Size = 14 , Color = ColorsHelper.BLACK };
            row.Shading.Color = ColorsHelper.RED_LIGHT;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.MergeRight = 2;
            row.Format.LeftIndent = 20;
        }

        private void AddHeaderForAmount (Cell row)
        {
            row.AddParagraph(ResourceReportGenerationMessages.AMOUNT);
            row.Format.Font = new Font { Name = FontHelper.RALEWAY_BLACK , Size = 14 , Color = ColorsHelper.WHITE };
            row.Shading.Color = ColorsHelper.RED_DARK;
            row.VerticalAlignment = VerticalAlignment.Center;
        }

        private void SetStyleBaseForExpenseInformation (Cell row)
        {
            row.Format.Font = new Font { Name = FontHelper.WORKSANS_REGULAR , Size = 12 , Color = ColorsHelper.BLACK };
            row.Shading.Color = ColorsHelper.GREEN_DARK;
            row.VerticalAlignment = VerticalAlignment.Center;
        }

        private void AddAmountForExpense(Cell row, decimal amount )
        {
            row.AddParagraph($"-{CURRENCY_SYBOL} {amount:f2}");
            row.Format.Font = new Font { Name = FontHelper.WORKSANS_REGULAR , Size = 14 , Color = ColorsHelper.BLACK };
            row.Shading.Color = ColorsHelper.WHITE;
            row.VerticalAlignment = VerticalAlignment.Center;
        }

        private void AddWhiteSpace(Table table )
        {
            var row = table.AddRow();
            row.Height = 25;
            row.Borders.Visible = false;
        }

        private byte[] RenderDocument( Document document )
        {
            var renderer = new PdfDocumentRenderer
            {
                Document = document ,
            };

            renderer.RenderDocument();

            using var file = new MemoryStream();
            renderer.PdfDocument.Save( file );

            return file.ToArray();

        }
    }
}
