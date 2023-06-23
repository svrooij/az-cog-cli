using System.CommandLine;
namespace AzCogCli;

class Program
{
    static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand("Azure Cognitive Search CLI");

        rootCommand.AddCommand(new Commands.Admin.AdminCommand());

        await rootCommand.InvokeAsync(args);
    }
}