using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

/// <summary>
/// Обработчик тестовых функций для проверки LaTeX документов
/// Теперь работает с DI
/// </summary>
public class TestFunctionHandler
{
    private readonly ILatexConfigurationService _configurationService;
    private readonly List<TestFunction> _testFunctions;

    public TestFunctionHandler(ILatexConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _testFunctions = new List<TestFunction>();
        InitializeTestFunctions();
    }

    /// <summary>
    /// Инициализация всех доступных тестовых функций
    /// </summary>
    private void InitializeTestFunctions()
    {
        _testFunctions.Add(new TestQuotationMarks(_configurationService));
        _testFunctions.Add(new TestHyphenInsteadOfDash(_configurationService));
        // Добавьте здесь другие тестовые функции по мере их обновления для DI
    }

    /// <summary>
    /// Запуск всех тестовых функций
    /// </summary>
    /// <returns>Словарь с результатами тестов</returns>
    public Dictionary<string, List<TestError>> RunAllTests()
    {
        var results = new Dictionary<string, List<TestError>>();

        foreach (var testFunction in _testFunctions)
        {
            var testName = testFunction.GetType().Name;
            results[testName] = new List<TestError>(testFunction.Errors);
        }

        return results;
    }

    /// <summary>
    /// Запуск конкретного теста по имени
    /// </summary>
    /// <param name="testName">Имя тестовой функции</param>
    /// <returns>Список ошибок или null если тест не найден</returns>
    public List<TestError>? RunSpecificTest(string testName)
    {
        var testFunction = _testFunctions.FirstOrDefault(t => t.GetType().Name == testName);
        return testFunction?.Errors;
    }

    /// <summary>
    /// Получение списка доступных тестов
    /// </summary>
    /// <returns>Список имен доступных тестов</returns>
    public List<string> GetAvailableTests()
    {
        return _testFunctions.Select(t => t.GetType().Name).ToList();
    }
} 