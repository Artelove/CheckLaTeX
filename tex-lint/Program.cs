using Microsoft.OpenApi.Models;
using TexLint.Models.HandleInfos;
using TexLint.Swagger;
using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Явно указываем Kestrel слушать порт 80. Это решает проблемы с Docker.
builder.WebHost.UseUrls("http://*:80");

// Add services to the container.
builder.Services.AddControllers();

// Регистрация LaTeX конфигурационных сервисов
builder.Services.AddScoped<ILatexConfigurationService, LatexConfigurationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "CheckLaTeX API", 
        Version = "v1",
        Description = "API для анализа и проверки LaTeX документов"
    });
    
    // Поддержка загрузки файлов в Swagger UI
    c.OperationFilter<FileUploadOperationFilter>();
});
// НЕ ИЗМЕНЯТЬ НА ДРУГОЕ. ВСЕГДА ДОЛЖНО БЫТЬ Development
builder.Environment.EnvironmentName = "Development";

var app = builder.Build();


// Конфигурация для отображения детальных ошибок в development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Глобальный обработчик исключений для API с детальным выводом
    app.UseExceptionHandler(appBuilder =>
    {
        appBuilder.Run(async context =>
        {
            var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (exceptionFeature != null)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(exceptionFeature.Error, "Unhandled exception occurred");

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = new
                    {
                        message = exceptionFeature.Error.Message,
                        stackTrace = exceptionFeature.Error.StackTrace,
                        type = exceptionFeature.Error.GetType().Name,
                        source = exceptionFeature.Error.Source,
                        innerException = exceptionFeature.Error.InnerException?.Message
                    },
                    timestamp = DateTimeOffset.UtcNow,
                    path = context.Request.Path
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
        });
    });
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CheckLaTeX API v1");
    c.RoutePrefix = "swagger"; // Swagger UI доступен по /swagger (без ведущего слеша)
});

app.UseAuthorization();
app.MapControllers();

app.Run();
