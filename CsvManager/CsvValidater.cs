using CsvHelper;
using CsvManager.Interfaces;
using CsvManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager
{
    /// <summary>
    /// CSV行のデータを検証するための基本的な実装クラスです。
    /// </summary>
    /// <typeparam name="TCsvModel">CSVの1行分のデータを表すモデルの型。</typeparam>
    public class CsvValidater<TCsvModel> : ICsvValidator<TCsvModel> where TCsvModel : class
    {
        /// <summary>
        /// CSVデータの1行を検証します。
        /// </summary>
        /// <param name="viewModel">検証対象の行データを表すモデル。</param>
        /// <param name="rowNumber">検証中のデータの行番号。</param>
        /// <returns>
        /// 検証結果を格納する <see cref="CsvImportResult"/>。エラーがある場合は失敗として結果を返し、
        /// 問題がなければ成功を返します。
        /// </returns>
        public virtual async Task<CsvImportResult> ValidateAsync(TCsvModel viewModel, int rowNumber)
        {
            // 検証結果を格納するリスト
            var validationResults = new List<ValidationResult>();

            // 検証コンテキストを生成
            var validationContext = new ValidationContext(viewModel);

            // エラー情報を格納するリスト
            var errors = new List<CsvError>();

            // モデルの検証を実行
            if (!Validator.TryValidateObject(viewModel, validationContext, validationResults, true))
            {
                foreach (var result in validationResults)
                {
                    // 各エラーをエラーリストに追加
                    errors.Add(new CsvError(rowNumber, result.ErrorMessage!));
                }
            }

            // 検証結果を返す。エラーがあれば失敗、なければ成功。
            return await Task.FromResult(errors.Count > 0 ? CsvImportResult.Failed(errors) : CsvImportResult.Success);
        }
    }

}
