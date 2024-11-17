using AutoMapper;
using CsvHelper;
using CsvManager.Exceptions;
using CsvManager.Interfaces;
using CsvManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace CsvManager
{
    /// <summary>
    /// CSVファイルを処理し、データベースにエンティティとして保存する機能を提供するクラス。
    /// </summary>
    /// <typeparam name="TDbContext">操作対象のデータベースコンテキストの型。</typeparam>
    /// <typeparam name="TCsvModel">CSV行を表すモデルの型。</typeparam>
    /// <typeparam name="TEntity">データベースエンティティの型。</typeparam>
    public class CsvImporter<TDbContext, TCsvModel, TEntity>
            where TDbContext : DbContext
            where TCsvModel : class
            where TEntity : class, new()
    {
        private readonly TDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CsvImporter<TDbContext, TCsvModel, TEntity>> _logger;

        /// <summary>
        /// CSV検証を実行するためのバリデーターのリスト。
        /// </summary>
        public IList<ICsvValidator<TCsvModel>> CsvValidators { get; } = new List<ICsvValidator<TCsvModel>>();

        /// <summary>
        /// CSV処理中に発生する例外を処理するための例外ハンドラ。
        /// </summary>
        public ICsvProcessingException CsvProcessingException { get; }

        /// <summary>
        /// CsvImporter クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="dbContext">使用するデータベースコンテキスト。</param>
        /// <param name="mapper">モデルとエンティティ間のマッピングを行う IMapper。</param>
        /// <param name="csvProcessingException">CSV処理例外を処理するハンドラ。</param>
        /// <param name="csvValidators">CSV検証を実行するためのバリデーターのコレクション。</param>
        public CsvImporter(
            TDbContext dbContext,
            IMapper mapper,
            ILogger<CsvImporter<TDbContext, TCsvModel, TEntity>> logger,
            ICsvProcessingException csvProcessingException = null!,
            IEnumerable<ICsvValidator<TCsvModel>> csvValidators = null!)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            CsvProcessingException = csvProcessingException ?? new CompositeCsvProcessingException(new ICsvProcessingException[]
            {
                new FormatExceptionHandler(),
                new CsvHelperExceptionHandler()
            });
            if (csvValidators is not null)
            {
                foreach (var v in csvValidators)
                {
                    CsvValidators.Add(v);
                }
            }
            else
            {
                // デフォルトの CsvValidater を使用
                CsvValidators.Add(new CsvValidater<TCsvModel>());
            }
        }

        /// <summary>
        /// 指定されたストリームからShift_JISエンコーディングでCSVリーダーを作成します。
        /// </summary>
        /// <param name="csvStream">CSVファイルのストリーム。</param>
        /// <returns>CsvReaderのインスタンス。</returns>
        private CsvReader CreateCsvReader(Stream csvStream) =>
            new CsvReader(new StreamReader(csvStream, Encoding.UTF8), CultureInfo.InvariantCulture);

        /// <summary>
        /// CSVファイルを処理し、エンティティをデータベースに保存します。
        /// </summary>
        /// <param name="csvStream">CSVデータのストリーム。</param>
        /// <param name="columnValues">エンティティに追加するカラム情報。</param>
        /// <param name="validateOnly">検証のみを実行する場合は true。</param>
        /// <returns>処理結果を示す CsvImportResult。</returns>
        public virtual async Task<CsvImportResult> ProcessCsvAsync(Stream csvStream, Dictionary<string, object> columnValues = null!, bool validateOnly = false)
        {
            var errors = new List<CsvError>();

            // InMemory データベースではトランザクションをスキップ
            var supportsTransaction = _dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

            using var transaction = supportsTransaction ? await _dbContext.Database.BeginTransactionAsync() : null;

            try
            {
                _logger.LogInformation("Starting CSV processing. Validate only: {ValidateOnly}", validateOnly);

                using var csvReader = CreateCsvReader(csvStream);

                var entities = new List<TEntity>();
                await foreach (var (cavModel, rowNumber) in ReadCsvRecordsAsync(csvReader, errors))
                {
                    // バリデーションを実行
                    foreach (var v in CsvValidators)
                    {
                        var result = await v.ValidateAsync(cavModel, rowNumber);
                        if (!result.Succeeded)
                        {
                            errors.AddRange(result.Errors);
                        }
                    }

                    if (!validateOnly)
                    {
                        entities.Add(_mapper.Map<TEntity>(cavModel));
                    }
                }

                if (!validateOnly && entities.Any())
                {
                    await _dbContext.Set<TEntity>().AddRangeAsync(entities);
                    await _dbContext.SaveChangesAsync();
                    //await transaction?.CommitAsync()!;
                    _logger.LogInformation("CSV processing completed successfully.");
                }
                if (errors.Any())
                {
                    if (supportsTransaction)
                    {
                        await transaction?.RollbackAsync()!;
                    }
                    _logger.LogWarning("Errors occurred during CSV processing. Number of errors: {ErrorCount}", errors.Count);
                    return CsvImportResult.Failed(errors);
                }  
            }
            catch (Exception ex)
            {
                if (supportsTransaction)
                {
                    await transaction?.RollbackAsync()!;
                }
                _logger.LogError(ex, "An exception occurred during CSV processing.");
                throw new Exception($"An error occurred: {ex.Message}", ex);
            }

            return CsvImportResult.Success;
        }


        /// <summary>
        /// CSVリーダーを非同期で読み取り、モデルと行番号を返します。
        /// </summary>
        /// <param name="csvReader">CSVデータを読み取る CsvReader。</param>
        /// <param name="errorMessages">エラー情報を格納するリスト。</param>
        /// <returns>CSVモデルと行番号のペアを含む非同期列挙。</returns>
        private async IAsyncEnumerable<(TCsvModel Record, int RowNumber)> ReadCsvRecordsAsync(CsvReader csvReader, List<CsvError> errorMessages)
        {
            int rowNumber = 1;
            while (await csvReader.ReadAsync())
            {
                TCsvModel record;
                try
                {
                    record = csvReader.GetRecord<TCsvModel>();
                }
                catch (Exception ex)
                {
                    errorMessages.Add(CsvProcessingException.HandleCsvProcessingException(ex, rowNumber));
                    _logger.LogWarning("Error occurred while reading the CSV file. Row: {RowNumber}, Error: {ErrorMessage}", rowNumber, ex.Message);
                    continue;
                }
                yield return (record, rowNumber++);
            }
        }
    }
}
