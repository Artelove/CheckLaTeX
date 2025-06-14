using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace TexLint.Swagger;

/// <summary>
/// Фильтр для правильного отображения загрузки файлов в Swagger UI
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IFormFile[]))
            .ToArray();

        if (fileParameters.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = {
                                ["zipFile"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "ZIP файл с LaTeX документами"
                                },
                                ["startFile"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Главный файл для анализа (опционально)"
                                }
                            },
                            Required = new HashSet<string> { "zipFile" }
                        }
                    }
                }
            };
        }
    }
} 