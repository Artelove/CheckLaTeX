using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TexLint.Models 
{
    public class RuleLoader 
    {
        public List<RuleDefinition> Rules { get; private set; }

        public RuleLoader() {
            Rules = new List<RuleDefinition>();
        }

        // Synchronous version
        public void LoadRules(string jsonFilePath) {
            if (!File.Exists(jsonFilePath)) {
                // In a real application, might throw an exception or log this.
                // For now, if the file doesn't exist, Rules will remain an empty list.
                System.Diagnostics.Debug.WriteLine($"Warning: Rules file not found at {jsonFilePath}");
                Rules = new List<RuleDefinition>(); 
                return;
            }
            try
            {
                string jsonData = File.ReadAllText(jsonFilePath);
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip // Allow comments in JSON
                };
                Rules = JsonSerializer.Deserialize<List<RuleDefinition>>(jsonData, options) ?? new List<RuleDefinition>();
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors
                System.Diagnostics.Debug.WriteLine($"Error deserializing rules.json: {ex.Message}");
                Rules = new List<RuleDefinition>(); // Or rethrow, or handle more gracefully
            }
            catch (Exception ex)
            {
                // Handle other potential errors like file access issues
                System.Diagnostics.Debug.WriteLine($"Error loading rules.json: {ex.Message}");
                Rules = new List<RuleDefinition>();
            }
        }

        // Async version (optional, but good practice for I/O)
        public async Task LoadRulesAsync(string jsonFilePath) {
             if (!File.Exists(jsonFilePath)) {
                System.Diagnostics.Debug.WriteLine($"Warning: Rules file not found at {jsonFilePath}");
                Rules = new List<RuleDefinition>();
                return;
            }
            try
            {
                string jsonData = await File.ReadAllTextAsync(jsonFilePath);
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                Rules = JsonSerializer.Deserialize<List<RuleDefinition>>(jsonData, options) ?? new List<RuleDefinition>();
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing rules.json: {ex.Message}");
                Rules = new List<RuleDefinition>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading rules.json: {ex.Message}");
                Rules = new List<RuleDefinition>();
            }
        }
    }
}
