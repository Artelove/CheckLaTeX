namespace TexLint.Models
{
    public class TextCommand : Command
    {
        public string Text = string.Empty;
        
        public const string TEXT_COMMAND_NAME = "TEXT_NAME";  

        public TextCommand(string fileName)
            :base(fileName)
        {
            Name = TEXT_COMMAND_NAME;
        }
        
        public override string ToString()
        {
            return $"{Text}";
        }
    }
}