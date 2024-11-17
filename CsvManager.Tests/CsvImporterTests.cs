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
    /// CsvImporter�N���X�̒P�̃e�X�g�����s���邽�߂̃e�X�g�N���X�B
    /// </summary>
    public class CsvImporterTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<ICsvProcessingException> _csvProcessingExceptionMock;
        private readonly Mock<ICsvValidator<TestCsvModel>> _validatorMock;
        private readonly TestDbContext _dbContext;
        private readonly Mock<ILogger<CsvImporter<TestDbContext, TestCsvModel, TestEntity>>> _loggerMock;
        /// <summary>
        /// CsvImporterTests �N���X�̐V�����C���X�^���X�����������܂��B
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
        /// �L����CSV�f�[�^���C���|�[�g�����ۂɁA�f�[�^�x�[�X�ɐ������ۑ�����邱�Ƃ��m�F���܂��B
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
        /// ������CSV�f�[�^���C���|�[�g�����ۂɁA�G���[���������Ԃ���邱�Ƃ��m�F���܂��B
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
    /// �e�X�g�p��CSV���f���N���X�BCSV��1�s��\�����܂��B
    /// </summary>
    public class TestCsvModel
    {
        /// <summary>
        /// ID��\����B
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ���O��\����B
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ���[���A�h���X��\����B
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// �e�X�g�p�̃f�[�^�x�[�X�G���e�B�e�B�N���X�B
    /// </summary>
    public class TestEntity
    {
        /// <summary>
        /// ID��\���v���p�e�B�B
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ���O��\���v���p�e�B�B
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ���[���A�h���X��\���v���p�e�B�B
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// �e�X�g�p�̃f�[�^�x�[�X�R���e�L�X�g�N���X�B
    /// </summary>
    public class TestDbContext : DbContext
    {
        /// <summary>
        /// TestDbContext �N���X�̐V�����C���X�^���X�����������܂��B
        /// </summary>
        /// <param name="options">DbContext�̃I�v�V�����B</param>
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        /// <summary>
        /// �e�X�g�G���e�B�e�B�̃f�[�^�Z�b�g�B
        /// </summary>
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }
}
