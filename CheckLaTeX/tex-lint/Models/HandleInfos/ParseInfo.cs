namespace TexLint.Models.HandleInfos
{
    
    public class ParseInfo
    {
        public string Name { get; set; }
        
        public TypeCount Param { get; set; }
        
        public TypeCount Arg { get; set; }
        
        public List<string> Order { get; set; }
        
        public bool IsCommandExist { get; set; } = true;
    }
}