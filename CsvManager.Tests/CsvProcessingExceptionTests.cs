using CsvHelper.Configuration;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager.Tests
{
    public class CsvProcessingExceptionTests
    {
        [Fact]
        public void HandleCsvProcessingException_ReturnsFormatError_WhenFormatExceptionThrown()
        {
            // Arrange
            var service = new CsvProcessingException();
            var exception = new FormatException();
            var rowNumber = 1;

            // Act
            var result = service.HandleCsvProcessingException(exception, rowNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(rowNumber, result.Row);
            Assert.Equal("Invalid format detected.", result.Description);
        }

        [Fact]
        public void HandleCsvProcessingException_ReturnsMissingFieldError_WhenMissingFieldExceptionThrown()
        {
            // Arrange
            var service = new CsvProcessingException();
            var context = new CsvContext(new CsvReader(new StringReader(string.Empty), new CsvConfiguration(new System.Globalization.CultureInfo("ja-JP"))));
            var exception = new CsvHelper.MissingFieldException(context, "Test message");
            var rowNumber = 2;

            // Act
            var result = service.HandleCsvProcessingException(exception, rowNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(rowNumber, result.Row);
            Assert.Equal("Missing fields in the CSV file.", result.Description);
        }

        [Fact]
        public void HandleCsvProcessingException_ReturnsUnknownError_WhenUnknownExceptionThrown()
        {
            // Arrange
            var service = new CsvProcessingException();
            var exception = new Exception();
            var rowNumber = 3;

            // Act
            var result = service.HandleCsvProcessingException(exception, rowNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(rowNumber, result.Row);
            Assert.Equal("An unknown error occurred.", result.Description);
        }
    }
}
