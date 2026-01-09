var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var storagePath = Path.Combine(builder.Environment.ContentRootPath, "Storage");
Directory.CreateDirectory(storagePath);

builder.Services.AddSingleton<FileSearchEngine.Domain.Interfaces.IInvertedIndex, FileSearchEngine.Infrastructure.Indexing.InMemoryInvertedIndex>();

builder.Services.AddSingleton<FileSearchEngine.Domain.Interfaces.ITextProcessor, FileSearchEngine.Infrastructure.TextProcessing.TextProcessor>();
builder.Services.AddSingleton<FileSearchEngine.Infrastructure.Repositories.Interfaces.IDocumentRepository, FileSearchEngine.Infrastructure.Repositories.Implementations.InMemoryDocumentRepository>();
builder.Services.AddSingleton<FileSearchEngine.Services.Interfaces.IFileStorageService>(sp => 
    new FileSearchEngine.Services.Implementations.FileStorageService(storagePath, sp.GetRequiredService<ILogger<FileSearchEngine.Services.Implementations.FileStorageService>>()));

builder.Services.AddHostedService<FileSearchEngine.Infrastructure.BackgroundServices.BackgroundIndexingService>();

builder.Services.AddScoped<FileSearchEngine.Services.Interfaces.ISearchService, FileSearchEngine.Services.Implementations.SearchService>();
builder.Services.AddScoped<FileSearchEngine.Services.Interfaces.IDocumentService, FileSearchEngine.Services.Implementations.DocumentService>();
builder.Services.AddSingleton<FileSearchEngine.Services.Interfaces.IIndexingService, FileSearchEngine.Services.Implementations.IndexingService>();

builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

Directory.CreateDirectory(storagePath);

app.Run();
