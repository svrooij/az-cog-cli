using System.CommandLine;
namespace AzCogCli.Commands.Admin;

public class AdminRootCommand: Command {
  public AdminRootCommand(): base("admin", "Administer your search service")
  {
    AddCommand(new CreateCommand());
  }
}