using AutoMapper;
using CsvHelper;
using CsvManager;
using CsvManager.Interfaces;
using CsvManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace CsvManager.Tests
{
    public class CsvMappingProfile : Profile
    {
        public CsvMappingProfile()
        {
            CreateMap<TestCsvModel, TestEntity>();
        }
    }

    /// <summary>
    /// CsvImporterクラスの単体テストを実行するためのテストクラス。
    /// </summary>
    public class CsvImporterTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<ICsvProcessingException> _csvProcessingExceptionMock;
        private readonly Mock<ICsvValidator<TestCsvModel>> _validatorMock;
        private readonly TestDbContext _dbContext;
        private readonly Mock<ILogger<CsvImporter<TestDbContext, TestCsvModel, TestEntity>>> _loggerMock;
        /// <summary>
        /// CsvImporterTests クラスの新しいインスタンスを初期化します。
        /// </summary>
        public CsvImporterTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CsvMappingProfile>();
            });
            _mapper = config.CreateMapper();
            _csvProcessingExceptionMock = new Mock<ICsvProcessingException>();
            _validatorMock = new Mock<ICsvValidator<TestCsvModel>>();
            _loggerMock = new Mock<ILogger<CsvImporter<TestDbContext, TestCsvModel, TestEntity>>>();
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _dbContext = new TestDbContext(options);
        }

        /// <summary>
        /// 有効なCSVデータをインポートした際に、データベースに正しく保存されることを確認します。
        /// </summary>
        [Fact]
        public async Task ProcessCsvAsync_ValidCsv_ShouldImportToDatabase()
        {
            // Arrange
            var csvData = "Id,Name,Email\n1,John Doe,john.doe@example.com\n2,Jane Smith,jane.smith@example.com";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvData));

            var importer = new CsvImporter<TestDbContext, TestCsvModel, TestEntity>(
                _dbContext,
                _mapper,
                 _loggerMock.Object,
                null!,
                null!);

            // Act
            var result = await importer.ProcessCsvAsync(stream, new Dictionary<string, object>(), validateOnly: false);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(2, await _dbContext.TestEntities.CountAsync());
        }

        /// <summary>
        /// 無効なCSVデータをインポートした際に、エラーが正しく返されることを確認します。
        /// </summary>
        [Fact]
        public async Task ProcessCsvAsync_InvalidCsv_ShouldReturnErrors()
        {
            // Arrange
            var csvData = "Id,Name,Email\n1,John Doe,john.doe@example.com\n2,Jane Smith,invalid-email";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvData));

            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<TestCsvModel>(), It.IsAny<int>()))
                          .ReturnsAsync((TestCsvModel model, int row) =>
                          {
                              if (model.Email == "invalid-email")
                              {
                                  return CsvImportResult.Failed(new[] { new CsvError(row, "Invalid email format") });
                              }
                              return CsvImportResult.Success;
                          });

            var importer = new CsvImporter<TestDbContext, TestCsvModel, TestEntity>(
                _dbContext,
                _mapper,
                 _loggerMock.Object,
                null!, 
                new[] { _validatorMock.Object });

            // Act
            var result = await importer.ProcessCsvAsync(stream, new Dictionary<string, object>(), validateOnly: false);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Single(result.Errors);
            Assert.Equal("Invalid email format", result.Errors.First().Description);
        }
    }

    /// <summary>
    /// テスト用のCSVモデルクラス。CSVの1行を表現します。
    /// </summary>
    public class TestCsvModel
    {
        /// <summary>
        /// IDを表す列。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 名前を表す列。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// メールアドレスを表す列。
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// テスト用のデータベースエンティティクラス。
    /// </summary>
    public class TestEntity
    {
        /// <summary>
        /// IDを表すプロパティ。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 名前を表すプロパティ。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// メールアドレスを表すプロパティ。
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// テスト用のデータベースコンテキストクラス。
    /// </summary>
    public class TestDbContext : DbContext
    {
        /// <summary>
        /// TestDbContext クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="options">DbContextのオプション。</param>
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        /// <summary>
        /// テストエンティティのデータセット。
        /// </summary>
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }
}
