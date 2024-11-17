using CsvHelper;
using CsvManager.Interfaces;
using CsvManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager.Exceptions
{
    public class CsvHelperExceptionHandler : ICsvProcessingException
    {
        public CsvError HandleCsvProcessingException(Exception exception, int rowNumber)
        {
            if (exception is CsvHelperException)
            {
                return new CsvError(rowNumber, "Missing fields in the CSV file.");
            }
            return null!;
        }
    }

}
