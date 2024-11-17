using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager.Models
{
    public class CsvImportResult
    {
        private static readonly CsvImportResult _success = new CsvImportResult
        {
            Succeeded = true
        };
        public bool Succeeded { get; protected set; }
        public static CsvImportResult Success => _success;

        public override string ToString()
        {
            if (!Succeeded)
            {
                return "Failed";
            }

            return "Succeeded";
        }

        private readonly List<CsvError> _errors = new List<CsvError>();
        public IEnumerable<CsvError> Errors => _errors;

        public static CsvImportResult Failed(params CsvError[] errors)
        {
            var result = new CsvImportResult { Succeeded = false };
            if (errors != null)
            {
                result._errors.AddRange(errors);
            }
            return result;
        }

        internal static CsvImportResult Failed(List<CsvError>? errors)
        {
            var result = new CsvImportResult { Succeeded = false };
            if (errors != null)
            {
                result._errors.AddRange(errors);
            }
            return result;
        }
    }


}
