using CsvManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager.Interfaces
{
    /// <summary>
    /// 特定のモデルで表現されるCSV行を検証するためのインターフェースです。
    /// </summary>
    /// <typeparam name="TCsvModel">1行分のCSVデータを表すモデルの型。</typeparam>
    public interface ICsvValidator<TCsvModel>
    {
        /// <summary>
        /// CSVデータの1行を検証します。
        /// </summary>
        /// <param name="viewModel">検証対象となる行データを表すモデル。</param>
        /// <param name="rowNumber">検証中のデータの行番号。</param>
        /// <returns>
        /// 検証結果を格納する <see cref="CsvImportResult"/>。エラーや成功状態を含みます。
        /// </returns>
        Task<CsvImportResult> ValidateAsync(TCsvModel viewModel, int rowNumber);
    }
}
