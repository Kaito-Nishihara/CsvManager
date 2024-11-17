using CsvManager.Interfaces;
using CsvManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManager
{
    public class CompositeCsvProcessingException : ICsvProcessingException
    {
        private readonly IEnumerable<ICsvProcessingException> _handlers;

        /// <summary>
        /// 複数の例外処理ロジックを登録するクラス。
        /// </summary>
        /// <param name="handlers">例外処理ロジックのコレクション。</param>
        public CompositeCsvProcessingException(IEnumerable<ICsvProcessingException> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public CsvError HandleCsvProcessingException(Exception exception, int rowNumber)
        {
            foreach (var handler in _handlers)
            {
                var result = handler.HandleCsvProcessingException(exception, rowNumber);
                if (result != null)
                {
                    return result;
                }
            }

            // どのハンドラでも処理されなかった場合のデフォルトエラー
            return new CsvError(rowNumber, $"未処理の例外: {exception.Message}");
        }
    }

}
