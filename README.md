
# **CsvManager**

A lightweight and flexible library for processing and importing CSV files into a database. The library supports validation, exception handling, and extensibility for a variety of use cases.

## **Features**

- **CSV Processing**: Parse CSV files and map them to database entities.
- **Validation**: Validate CSV rows with customizable validation logic.
- **Exception Handling**: Handle errors during processing with a composable exception handling mechanism.
- **Extensibility**: Easily extend the library with custom validators and exception handlers.
- **Database Support**: Compatible with Entity Framework Core.

---

## **Getting Started**

### **Installation**

To use `CsvManager`, add the library to your .NET project:

```bash
dotnet add package CsvManager
```

### **Prerequisites**

- .NET 8.0 or later
- Entity Framework Core
- AutoMapper

---

## **Usage**

### **1. Define Your Models**
Create a model for the CSV file rows and a corresponding database entity.

```csharp
public class CsvRowModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class DatabaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### **2. Configure AutoMapper**
Create a mapping profile for converting CSV models to database entities.

```csharp
public class CsvMappingProfile : Profile
{
    public CsvMappingProfile()
    {
        CreateMap<CsvRowModel, DatabaseEntity>();
    }
}
```

### **3. Set Up Your Database Context**
Define a DbContext for your application.

```csharp
public class AppDbContext : DbContext
{
    public DbSet<DatabaseEntity> Entities { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
```

### **4. Use CsvImporter**
Use `CsvImporter` to process the CSV file and save the data to the database.

```csharp
var importer = new CsvImporter<AppDbContext, CsvRowModel, DatabaseEntity>(
    dbContext,
    mapper,
    new CompositeCsvProcessingException(new List<ICsvProcessingException>
    {
        new FormatExceptionHandler(),
        new CsvHelperExceptionHandler()
    }),
    new List<ICsvValidator<CsvRowModel>> { new CsvValidater<CsvRowModel>() }
);

using var csvStream = new FileStream("example.csv", FileMode.Open);
var result = await importer.ProcessCsvAsync(csvStream, new Dictionary<string, object>(), validateOnly: false);

if (result.Succeeded)
{
    Console.WriteLine("CSV import succeeded!");
}
else
{
    Console.WriteLine($"CSV import failed with {result.Errors.Count} errors.");
}
```

---

## **Extending the Library**

### **Custom Validator**
Create a custom validator to add additional validation logic.

```csharp
public class CustomValidator : ICsvValidator<CsvRowModel>
{
    public Task<CsvImportResult> ValidateAsync(CsvRowModel model, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
        {
            return Task.FromResult(CsvImportResult.Failed(new List<CsvError>
            {
                new CsvError(rowNumber, "Invalid email format.")
            }));
        }
        return Task.FromResult(CsvImportResult.Success);
    }
}
```

### **Custom Exception Handler**
Handle specific exceptions during CSV processing.

```csharp
public class CustomExceptionHandler : ICsvProcessingException
{
    public CsvError HandleCsvProcessingException(Exception exception, int rowNumber)
    {
        if (exception is NullReferenceException)
        {
            return new CsvError(rowNumber, "A null value was encountered.");
        }
        return new CsvError(rowNumber, $"Unexpected error: {exception.Message}");
    }
}
```

---
