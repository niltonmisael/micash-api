using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.Expenses;
using MiCash.Application.UseCases.Expenses.Register;
using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Validators.Tests.Expenses.Register
{
    public class RegisterExpenseValidatorTests
    {
        [Fact] 
        public void Sucesso ()
        {
            //Arrange
            var validador = new ExpensesValidator();
            var request = RequestExpenseJsonBuilder.Build();

            //Act
            var result = validador.Validate(request);

            //Assert
            result.IsValid.Should().BeTrue();
        }   
    }
}
