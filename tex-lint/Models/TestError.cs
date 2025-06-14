namespace TexLint.Models
{
    public class TestError
    {
        public Command? ErrorCommand = null;

        private string _errorInfo;
        public string ErrorInfo
        {
            get => _errorInfo;
            set => _errorInfo = GetCommandLocationInfo() + value;
        }
        public ErrorType ErrorType;

        // НОВЫЕ поля для поддержки диагностик VS Code
        /// <summary>
        /// Имя файла относительно корня проекта (например, "chapter1/main.tex")
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Номер строки (1-based) где найдена ошибка
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// Номер колонки (1-based) где начинается ошибка
        /// </summary>
        public int? ColumnNumber { get; set; }

        /// <summary>
        /// Номер строки (1-based) где заканчивается ошибка (опционально)
        /// </summary>
        public int? EndLineNumber { get; set; }

        /// <summary>
        /// Номер колонки (1-based) где заканчивается ошибка (опционально)
        /// </summary>
        public int? EndColumnNumber { get; set; }

        /// <summary>
        /// Исходный текст с ошибкой для контекста
        /// </summary>
        public string? OriginalText { get; set; }

        /// <summary>
        /// Предлагаемое исправление (опионально)
        /// </summary>
        public string? SuggestedFix { get; set; }

        public void ConsolePrint()
        {
            Console.WriteLine(ErrorType + " --- " + ErrorInfo);
        }

        public override string ToString()
        {
            return ErrorType + " --- " + ErrorInfo + "\n";
        }

        private string GetCommandLocationInfo()
        {
            if (ErrorCommand == null)
                return string.Empty;
            
            return $"\t|File:\"{ErrorCommand.FileOwner}\";String number:{ErrorCommand.StringNumber}| ";
        }

        /// <summary>
        /// Создает ошибку с диагностической информацией
        /// </summary>
        public static TestError CreateWithDiagnostics(
            ErrorType errorType, 
            string errorInfo, 
            string fileName, 
            int lineNumber, 
            int columnNumber, 
            string originalText,
            int? endLineNumber = null, 
            int? endColumnNumber = null, 
            string? suggestedFix = null,
            Command? errorCommand = null)
        {
            return new TestError
            {
                ErrorType = errorType,
                _errorInfo = errorInfo, // Прямое присваивание чтобы избежать добавления GetCommandLocationInfo()
                FileName = fileName,
                LineNumber = lineNumber,
                ColumnNumber = columnNumber,
                EndLineNumber = endLineNumber,
                EndColumnNumber = endColumnNumber,
                OriginalText = originalText,
                SuggestedFix = suggestedFix,
                ErrorCommand = errorCommand
            };
        }
    }
}