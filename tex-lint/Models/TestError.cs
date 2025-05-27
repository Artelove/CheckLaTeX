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
    }
}