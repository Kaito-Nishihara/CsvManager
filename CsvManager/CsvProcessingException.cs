using CsvManager.Interfaces;
using CsvManager.Models;

namespace CsvManager
{
    /// <summary>
    /// CSV処理中に発生する例外を処理する基本的な実装クラス。
    /// </summary>
    public class CsvProcessingException : ICsvProcessingException
    {
        /// <summary>
        /// 発生した例外を処理し、エラーメッセージをカスタマイズした <see cref="CsvError"/> を生成します。
        /// </summary>
        /// <param name="exception">処理中に発生した例外。</param>
        /// <param name="rowNumber">エラーが発生したCSVの行番号。</param>
        /// <returns>
        /// エラー情報を格納した <see cref="CsvError"/> オブジェクト。
        /// 例外の種類に応じたカスタマイズされたエラーメッセージが含まれます。
        /// </returns>
        public virtual CsvError HandleCsvProcessingException(Exception exception, int rowNumber)
        {
            if (exception is FormatException)
            {
                return new CsvError(rowNumber, "Invalid format detected.");
            }
            else if (exception is CsvHelper.MissingFieldException)
            {
                return new CsvError(rowNumber, "Missing fields in the CSV file.");
            }
            return new CsvError(rowNumber, "An unknown error occurred.");
        }
    }
}
