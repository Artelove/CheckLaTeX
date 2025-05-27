namespace TexLint.Models.HandleInfos
{
    public struct TypeCount
    {
        private const string PHRASE_TYPE = "phrase";
        private const string VALUE_TYPE = "value";
        
        public string Type
        {
            set
            {
                switch (value)
                {
                    case PHRASE_TYPE: ParseType = ParameterParseType.Phrase; break;
                    case VALUE_TYPE: ParseType = ParameterParseType.Value; break;
                }
            }
        }

        public string Count { get; set; }
        
        public ParameterParseType ParseType;
    }

}