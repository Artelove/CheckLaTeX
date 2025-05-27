using System.Collections.Generic;
// Using System.Text.Json.Serialization; // For JsonPropertyName if needed (not strictly needed for this definition)

namespace TexLint.Models {
    public class RuleDefinition {
        public string RuleId { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Could be an enum: Error, Warning, Info
        public string TestFunctionName { get; set; } = string.Empty; // Maps to the C# TestFunction class
        public Dictionary<string, object> Parameters { get; set; }
        public string ErrorMessage { get; set; } = string.Empty; // Template
        public string Suggestion { get; set; } = string.Empty; // Template

        public RuleDefinition() { 
            Parameters = new Dictionary<string, object>();
        }
    }
}
