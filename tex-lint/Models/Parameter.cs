namespace TexLint.Models
{
    public class Parameter
    {
        public string Text = string.Empty;

        public string Value = string.Empty;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Value) ? Text : $"{Text}={Value}";
        }
    }

}