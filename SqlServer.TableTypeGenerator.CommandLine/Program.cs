using SqlServer.TableTypeGenerator;
using System.CommandLine;

var command = new RootCommand("Generate SQL Server table types from table definitions.");
var typesDirArgument = new Argument<string>() { Name = "Tables directory" };
var tablesFilterOption = new Option<string>("--tables", getDefaultValue: () => string.Empty);
var addColumnToTableOptions = new Option<IEnumerable<string>>("--add-col-to-table", getDefaultValue: () => Enumerable.Empty<string>()) { AllowMultipleArgumentsPerToken = true };
var addFromTableOptions = new Option<IEnumerable<string>>("--add-from-table", getDefaultValue: () => Enumerable.Empty<string>()) { AllowMultipleArgumentsPerToken = true };
var renamedTypesOption = new Option<string>("--renamed-types", getDefaultValue: () => string.Empty);

command.Add(typesDirArgument);
command.Add(tablesFilterOption);
command.Add(addColumnToTableOptions);
command.Add(addFromTableOptions);
command.Add(renamedTypesOption);
command.SetHandler((tablesDir, tablesFilter, addColumnToTables, addFromTables, renamedTypesStr) => 
{
    var additionalColumnsByType = addColumnToTables
        .ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1].Split(','));

    var addFromTablesByType = addFromTables.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1].Split(','));
    var tables = tablesFilter.Split(',').ToList();
    var renamedTypes = renamedTypesStr.Split(',').ToDictionary(rn => rn.Split('=')[0], rn => rn.Split('=')[1]);
    var ttGenerator = new TableTypesGenerator(tablesDir, new TableTypeGenerationOptions(tables, additionalColumnsByType, addFromTablesByType, renamedTypes));
    ttGenerator.WriteFiles();
}, typesDirArgument, tablesFilterOption, addColumnToTableOptions, addFromTableOptions, renamedTypesOption);

await command.InvokeAsync(args);