using CsvManager.Interfaces;
using CsvManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager.Exceptions
{
    public class FormatExceptionHandler : ICsvProcessingException
    {
        public CsvError HandleCsvProcessingException(Exception exception, int rowNumber)
        {
            if (exception is FormatException)
            {
                return new CsvError(rowNumber, "Invalid format detected.");
            }
            return null!;
        }
    }

}
