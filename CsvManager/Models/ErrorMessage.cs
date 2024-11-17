using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager.Models
{
    public class CsvError
    {
        public CsvError(int row, string description) 
        {
            Row = row;
            Description = description;
        }

        public int Row { get; set; } = default!;

        public string Description { get; set; } = default!;
    }
}
