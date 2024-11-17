using CsvHelper;
using CsvManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager.Interfaces
{
    /// <summary>
    /// CSV処理中に発生する例外をカスタマイズして処理するためのインターフェース。
    /// </summary>
    public interface ICsvProcessingException
    {
        /// <summary>
        /// 発生した例外を処理し、カスタマイズされたエラーメッセージを含む <see cref="CsvError"/> を生成します。
        /// </summary>
        /// <param name="exception">処理中に発生した例外。</param>
        /// <param name="rowNumber">エラーが発生したCSVの行番号。</param>
        /// <returns>
        /// エラー情報を格納した <see cref="CsvError"/> オブジェクト。
        /// </returns>
        public CsvError HandleCsvProcessingException(Exception exception, int rowNumber);
    }
}
