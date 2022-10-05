using DotNet.GenerateTableTypes;
using System.CommandLine;

var command = new RootCommand("Generate SQL Server table types from table definitions.");
var typesDirArgument = new Argument<string>() { Name = "Tables directory" };
var typeOptionsOption = new Option<IEnumerable<string>>("--type-options") { AllowMultipleArgumentsPerToken = true };
command.Add(typesDirArgument);
command.Add(typeOptionsOption);
command.SetHandler((tablesDir, typesOptions) => 
{ 
    var ttGenerator = new TableTypesGenerator(tablesDir, typesOptions);
    ttGenerator.WriteFiles();
}, typesDirArgument, typeOptionsOption);

await command.InvokeAsync(args);