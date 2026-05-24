using Microsoft.AspNetCore.Hosting;

namespace YoutubeToLinkedIn.Api.Services;

public class PromptLoader
{
    private readonly Dictionary<string, string> _prompts;

    public PromptLoader(IWebHostEnvironment env)
    {
        var promptsDir = Path.Combine(env.ContentRootPath, "Prompts");

        if (!Directory.Exists(promptsDir))
            throw new DirectoryNotFoundException($"Prompts directory not found: {promptsDir}");

        _prompts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(promptsDir, "*.md"))
        {
            var key = Path.GetFileNameWithoutExtension(file);
            _prompts[key] = File.ReadAllText(file);
        }
    }

    public string GetPrompt(string name)
    {
        if (_prompts.TryGetValue(name, out var content))
            return content;

        throw new KeyNotFoundException($"Prompt '{name}' not found. Available prompts: {string.Join(", ", _prompts.Keys)}");
    }
}
