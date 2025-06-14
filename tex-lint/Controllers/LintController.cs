using Microsoft.AspNetCore.Mvc;
using TexLint.Models;
using TexLint.TestFunctionClasses;
using System.Text;
using System.IO.Compression;
using System.Text.Json;
using TexLint.Models.HandleInfos;

namespace TexLint.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LintController : ControllerBase
{
    private readonly ILogger<LintController> _logger;
    private readonly ILatexConfigurationService _configurationService;

    public LintController(ILogger<LintController> logger, ILatexConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
    }

    /// <summary>
    /// Анализ LaTeX документов из JSON запроса
    /// </summary>
    /// <param name="request">Запрос с файлами LaTeX</param>
    /// <returns>Результат анализа команд</returns>
    [HttpPost("analyze")]
    public async Task<ActionResult<string>> Analyze([FromBody] LintRequest request)
    {
        string? workingDirectory = null;
        try
        {
            if (request?.Files == null || !request.Files.Any())
            {
                return BadRequest("Файлы для анализа не предоставлены");
            }

            if (string.IsNullOrEmpty(request.StartFile))
            {
                return BadRequest("Стартовый файл не указан");
            }

            workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingDirectory);

            foreach (var kv in request.Files)
            {
                var filePath = Path.Combine(workingDirectory, kv.Key);
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(filePath, kv.Value);
            }

            var handler = new CommandHandler(request.StartFile, workingDirectory, _configurationService);
            var commands = handler.FindAllCommandsInDocument();
            var builder = new StringBuilder();
            foreach (var command in commands)
            {
                builder.Append(command.ToString());
            }

            return builder.ToString();
        }
        catch (LatexParsingException latexEx)
        {
            _logger.LogError(latexEx, "Ошибка парсинга LaTeX файлов");
            
            return BadRequest(new
            {
                error = "Ошибка парсинга LaTeX документа",
                message = latexEx.Message,
                fileName = latexEx.FileName,
                lineNumber = latexEx.LineNumber,
                characterPosition = latexEx.CharacterPosition,
                latexContext = latexEx.LatexContext,
                type = "LatexParsingException"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при анализе LaTeX файлов");
            
            // В development режиме возвращаем подробную информацию об ошибке
            var isDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            
            if (isDevelopment)
            {
                return StatusCode(500, new
                {
                    error = "Ошибка при анализе LaTeX файлов",
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    type = ex.GetType().Name,
                    innerException = ex.InnerException?.Message
                });
            }
            else
            {
                return StatusCode(500, new { error = "Внутренняя ошибка сервера при анализе файлов" });
            }
        }
        finally
        {
            // Очищаем временную директорию с retry логикой
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                await CleanupDirectoryAsync(workingDirectory);
            }
        }
    }

    /// <summary>
    /// Анализ LaTeX документов из ZIP архива
    /// </summary>
    /// <param name="zipFile">ZIP файл с LaTeX документами</param>
    /// <param name="startFile">Главный файл для начала анализа (опционально)</param>
    /// <returns>Результат анализа команд</returns>
    [HttpPost("analyze-zip")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<string>> AnalyzeZip(
        [FromForm] IFormFile zipFile, 
        [FromForm] string? startFile = null)
    {
        try
        {
            if (zipFile == null || zipFile.Length == 0)
            {
                return BadRequest("ZIP файл не предоставлен или пустой");
            }

            if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Поддерживаются только ZIP файлы");
            }

            // Создаем временную директорию
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TEST");
            Directory.CreateDirectory(workingDirectory);

            try
            {
                // Сохраняем и извлекаем ZIP файл
                var extractPath = workingDirectory;

                // Используем memory stream вместо временного файла для уменьшения блокировок
                using (var memoryStream = new MemoryStream())
                {
                    await zipFile.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Извлекаем файлы напрямую из памяти
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name)) // Это директория
                                continue;

                            // Убираем первую папку из пути, если она есть
                            string entryPath = entry.FullName;
                            string[] pathParts = entryPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            
                            if (pathParts.Length > 1)
                            {
                                entryPath = String.Join(Path.DirectorySeparatorChar, pathParts.Skip(1));
                            }

                            var fullPath = Path.Combine(extractPath, entryPath);
                            var directory = Path.GetDirectoryName(fullPath);

                            if (!string.IsNullOrEmpty(directory))
                                Directory.CreateDirectory(directory);

                            // Извлекаем файл с правильным закрытием потоков
                            using (var entryStream = entry.Open())
                            using (var outputStream = System.IO.File.Create(fullPath))
                            {
                                await entryStream.CopyToAsync(outputStream);
                            }
                        }
                    }
                }

                // Определяем стартовый файл
                string resolvedStartFile;
                if (!string.IsNullOrEmpty(startFile))
                {
                    resolvedStartFile = startFile;
                }
                else
                {
                    // Ищем главный файл автоматически
                    resolvedStartFile = FindMainTexFile(extractPath);
                    if (string.IsNullOrEmpty(resolvedStartFile))
                    {
                        return BadRequest("Не удалось найти главный .tex файл. Укажите startFile параметр.");
                    }
                }

                // Проверяем существование стартового файла
                var startFilePath = Path.Combine(extractPath, resolvedStartFile);
                if (!System.IO.File.Exists(startFilePath))
                {
                    return BadRequest($"Стартовый файл '{resolvedStartFile}' не найден в архиве");
                }

                // Анализируем документы
                var handler = new CommandHandler(resolvedStartFile, extractPath, _configurationService);
                var commands = handler.FindAllCommandsInDocument();

                // Настраиваем пути для тестовых функций
                TestUtilities.StartDirectory = extractPath;
                TestUtilities.FoundsCommands = commands;
                TestUtilities.FoundsCommandsWithLstlisting = commands;

                // Запускаем тестовые функции для проверки
                var testHandler = new TestFunctionClasses.TestFunctionHandler(_configurationService);
                var results = testHandler.RunAllTests();
                


                var builder = new StringBuilder();

                builder.AppendLine($"=== Анализ ZIP архива: {zipFile.FileName} ===");
                builder.AppendLine($"Стартовый файл: {resolvedStartFile}");
                builder.AppendLine($"Найдено команд: {commands.Count}");
                builder.AppendLine();

                builder.AppendLine("=== РЕЗУЛЬТАТЫ ПРОВЕРКИ ===");
                // Результаты уже выводятся TestFunctionHandler в консоль и файл
                builder.AppendLine("Детальный отчет сохранен в папку CheckReports");
                builder.AppendLine();

                builder.AppendLine("=== СПИСОК КОМАНД ===");
                foreach (var command in commands)
                {
                    builder.AppendLine(command.ToString());
                }

                return Ok(new
                {
                    commandsFound = commands.Count,
                    testResults = results.Select(r => new
                    {
                        testName = r.Key,
                        errors = r.Value.Select(e => new
                        {
                            type = e.ErrorType.ToString(),
                            info = e.ErrorInfo,
                            command = e.ErrorCommand?.ToString(),
                            // Заменяем временные пути на относительные пути в архиве
                            fileName = ReplaceZipTemporaryPath(e.FileName, extractPath, resolvedStartFile),
                            lineNumber = e.LineNumber,
                            columnNumber = e.ColumnNumber,
                            endLineNumber = e.EndLineNumber,
                            endColumnNumber = e.EndColumnNumber,
                            originalText = e.OriginalText,
                            suggestedFix = e.SuggestedFix
                        })
                    })
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e + " " + e.Message + " " + e.StackTrace);
                return StatusCode(500, new { error = e.Message });
            }
            finally
            {
                // Очищаем временную директорию с retry логикой
                await CleanupDirectoryAsync(workingDirectory);
            }
        }
        catch (LatexParsingException latexEx)
        {
            _logger.LogError(latexEx, "Ошибка парсинга LaTeX в файле: {FileName}", zipFile?.FileName);
            
            return BadRequest(new
            {
                error = "Ошибка парсинга LaTeX документа",
                message = latexEx.Message,
                fileName = latexEx.FileName,
                lineNumber = latexEx.LineNumber,
                characterPosition = latexEx.CharacterPosition,
                latexContext = latexEx.LatexContext,
                type = "LatexParsingException"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке ZIP файла: {FileName}", zipFile?.FileName);
            
            // В development режиме возвращаем подробную информацию об ошибке
            var isDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            
            if (isDevelopment)
            {
                return StatusCode(500, new
                {
                    error = "Ошибка при обработке ZIP файла",
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    type = ex.GetType().Name,
                    innerException = ex.InnerException?.Message
                });
            }
            else
            {
                return StatusCode(500, new { error = "Внутренняя ошибка сервера при обработке ZIP файла" });
            }
        }
    }

    /// <summary>
    /// Поиск главного .tex файла в директории
    /// </summary>
    /// <param name="directory">Директория для поиска</param>
    /// <returns>Относительный путь к главному файлу или null</returns>
    private string? FindMainTexFile(string directory)
    {
        var texFiles = Directory.GetFiles(directory, "*.tex", SearchOption.AllDirectories);
        
        // Ищем файлы с \documentclass
        foreach (var texFile in texFiles)
        {
            try
            {
                var content = System.IO.File.ReadAllText(texFile);
                if (content.Contains("\\documentclass"))
                {
                    return Path.GetRelativePath(directory, texFile);
                }
            }
            catch
            {
                // Игнорируем ошибки чтения файлов
            }
        }

        // Если не найден, возвращаем первый .tex файл
        if (texFiles.Length > 0)
        {
            return Path.GetRelativePath(directory, texFiles[0]);
        }

        return null;
    }

    [HttpPost("check")]
    public IActionResult CheckLatex([FromBody] LatexCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Получен запрос на проверку LaTeX документа");

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Содержимое документа не может быть пустым" });
            }

            // Создаем временный файл
            var tempFile = Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFile, request.Content);

            try
            {
                var tempDir = Path.GetDirectoryName(tempFile);
                var fileName = Path.GetFileName(tempFile);

                // Создаем CommandHandler с DI
                var commandHandler = new CommandHandler(fileName, tempDir, _configurationService);
                var commands = commandHandler.FindAllCommandsInDocument();

                // Заполняем TestUtilities для использования в тестовых функциях
                TestUtilities.FoundsCommandsWithLstlisting = commands;

                // Создаем и запускаем тестовые функции с DI
                var testFunctionHandler = new TestFunctionClasses.TestFunctionHandler(_configurationService);
                var results = testFunctionHandler.RunAllTests();

                // Определяем путь файла для отображения в диагностике
                var displayFilePath = request.FilePath ?? "document.tex";
                var tempFileFullPath = Path.Combine(tempDir, fileName);

                return Ok(new
                {
                    commandsFound = commands.Count,
                    testResults = results.Select(r => new
                    {
                        testName = r.Key,
                        errors = r.Value.Select(e => new
                        {
                            type = e.ErrorType.ToString(),
                            info = e.ErrorInfo,
                            command = e.ErrorCommand?.ToString(),
                            // Заменяем временный путь на оригинальный путь пользователя
                            fileName = ReplaceTemporaryPath(e.FileName, tempFileFullPath, displayFilePath),
                            lineNumber = e.LineNumber,
                            columnNumber = e.ColumnNumber,
                            endLineNumber = e.EndLineNumber,
                            endColumnNumber = e.EndColumnNumber,
                            originalText = e.OriginalText,
                            suggestedFix = e.SuggestedFix
                        })
                    })
                });
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке LaTeX документа");
            
            // В development режиме возвращаем подробную информацию об ошибке
            var isDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            
            if (isDevelopment)
            {
                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка сервера",
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    type = ex.GetType().Name,
                    innerException = ex.InnerException?.Message
                });
            }
            else
            {
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }
    }

    /// <summary>
    /// Заменяет временный путь файла на оригинальный путь пользователя
    /// </summary>
    /// <param name="currentPath">Текущий путь (может быть временным)</param>
    /// <param name="tempFilePath">Путь к временному файлу</param>
    /// <param name="originalPath">Оригинальный путь пользователя</param>
    /// <returns>Корректный путь для отображения</returns>
    private string? ReplaceTemporaryPath(string? currentPath, string tempFilePath, string originalPath)
    {
        if (string.IsNullOrEmpty(currentPath))
            return originalPath;

        // Если путь совпадает с временным файлом, заменяем на оригинальный
        if (currentPath == tempFilePath || 
            Path.GetFileName(currentPath) == Path.GetFileName(tempFilePath))
        {
            return originalPath;
        }

        // Если путь содержит временную директорию, заменяем её на путь пользователя
        var tempDir = Path.GetDirectoryName(tempFilePath);
        if (!string.IsNullOrEmpty(tempDir) && currentPath.StartsWith(tempDir))
        {
            var relativePath = Path.GetRelativePath(tempDir, currentPath);
            return Path.Combine(Path.GetDirectoryName(originalPath) ?? "", relativePath);
        }

        return currentPath;
    }

    /// <summary>
    /// Заменяет временный путь файла на относительный путь в ZIP архиве
    /// </summary>
    /// <param name="currentPath">Текущий путь (может быть временным)</param>
    /// <param name="extractPath">Путь к временной директории извлечения</param>
    /// <param name="startFile">Стартовый файл</param>
    /// <returns>Относительный путь для отображения</returns>
    private string? ReplaceZipTemporaryPath(string? currentPath, string extractPath, string startFile)
    {
        if (string.IsNullOrEmpty(currentPath))
            return startFile;

        // Если путь находится в временной директории, создаем относительный путь
        if (currentPath.StartsWith(extractPath))
        {
            return Path.GetRelativePath(extractPath, currentPath);
        }

        return currentPath;
    }

    /// <summary>
    /// Безопасная очистка временной директории с retry логикой
    /// </summary>
    /// <param name="directoryPath">Путь к директории для удаления</param>
    private async Task CleanupDirectoryAsync(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            return;

        const int maxRetries = 3;
        const int delayMs = 500;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Сначала попытаемся освободить файлы
                await ForceUnlockFilesAsync(directoryPath);
                
                // Затем удаляем директорию
                Directory.Delete(directoryPath, true);
                
                _logger.LogDebug("Временная директория успешно удалена: {DirectoryPath}", directoryPath);
                return;
            }
            catch (IOException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("Попытка {Attempt}/{MaxRetries} удаления директории неудачна: {Error}. Повторная попытка через {Delay}мс",
                    attempt, maxRetries, ex.Message, delayMs);
                
                await Task.Delay(delayMs * attempt); // Увеличиваем задержку с каждой попыткой
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при удалении временной директории: {DirectoryPath}", directoryPath);
                break;
            }
        }

        // Если все попытки неудачны, логируем предупреждение
        _logger.LogWarning("Не удалось удалить временную директорию после {MaxRetries} попыток: {DirectoryPath}. " +
                          "Файлы будут удалены автоматически системой очистки temp.", maxRetries, directoryPath);
    }

    /// <summary>
    /// Принудительное освобождение файлов в директории
    /// </summary>
    /// <param name="directoryPath">Путь к директории</param>
    private async Task ForceUnlockFilesAsync(string directoryPath)
    {
        try
        {
            // Получаем все файлы в директории
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                try
                {
                    // Снимаем атрибут ReadOnly если есть
                    var attributes = System.IO.File.GetAttributes(file);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        System.IO.File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Не удалось изменить атрибуты файла {FilePath}: {Error}", file, ex.Message);
                }
            }

            // Небольшая задержка для завершения файловых операций
            await Task.Delay(100);
            
            // Принудительно вызываем сборщик мусора для освобождения файловых дескрипторов
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Ошибка при принудительном освобождении файлов: {Error}", ex.Message);
        }
    }
}
