using System.CommandLine;
namespace AzCogCli.Commands.Admin;

public class AdminCommand: Command {
  public AdminCommand(): base("admin")
  {
    AddCommand(new CreateCommand());
  }
}