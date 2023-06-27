using System.CommandLine;
namespace AzCogCli.Commands.Index;

public class IndexRootCommand: Command {
  public IndexRootCommand(): base("index", "Index data")
  {
    AddCommand(new IndexFolderCommand());
  }
}