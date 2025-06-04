using System.Diagnostics;
using TexLint;
using TexLint.TestFunctionClasses;
using TexLint.Controllers;
using TexLint.Models;

class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Enter start folder name");
        var startFile = "Thesis.tex";
        string? startDirectory = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            Console.WriteLine("Путь не может быть пустым");
            return;
        }
        TestUtilities.StartDirectory = startDirectory;
        var controller = new LintController(startFile, startDirectory);
        var commands = controller.Analyze();
        foreach (var command in commands)
        {
            Console.WriteLine(command);
        }
        Console.ReadLine();
    }
}