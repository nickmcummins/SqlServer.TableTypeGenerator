using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using static DotNet.GenerateTableTypes.Constants;

namespace DotNet.GenerateTableTypes
{
    public class TableTypeGenerator
    {
        private string TablesDir { get; }
        private IEnumerable<string> TableSqlFiles { get; }
        public IDictionary<string, string> TableTypeDefinitions { get; }

        public TableTypeGenerator(string tablesDir)
        {
            TablesDir = tablesDir;
            TableSqlFiles = Utils.GetDirectories(tablesDir)
                .SelectMany(dir => Directory.GetFiles(dir).Where(filename => !filename.EndsWith("_History.sql")));
            TableTypeDefinitions = Generate();
        }

        public IDictionary<string, string> Generate()
        {
            var tables = new Dictionary<string, string>();
            foreach (var tableSqlFile in TableSqlFiles)
            {
                var parsedSql = Parser.Parse(File.ReadAllText(tableSqlFile)).Script.Children
                    .Flatten(child => child.Children)
                    .Where(child => child is SqlCreateTableStatement)
                    .Select(child => child as SqlCreateTableStatement);

                var tableDefinition = parsedSql.First().Definition;
                tables[$"{Path.GetFileNameWithoutExtension(tableSqlFile)}Type"] = string.Join(",\n", tableDefinition.ColumnDefinitions.Select(colDef => $"\t{colDef.Name} {FormatTypeString(colDef.DataType.Sql)}"));
            }


            return tables.ToDictionary(kv => kv.Key, kv => $"CREATE TYPE [clm].[{kv.Key}] AS TABLE (\n{kv.Value}\n)");
        }

        public void WriteFiles()
        {
            var typesDir = TablesDir.Replace("Tables", "GeneratedTypes");
            if (!Directory.Exists(typesDir))
            {
                Directory.CreateDirectory(typesDir);
            }
            foreach (var tableType in TableTypeDefinitions)
            {
                var typeFilename = $@"{typesDir}\{tableType.Key}.sql";
                File.WriteAllText(typeFilename, tableType.Value);
                Console.Out.WriteLine($"\tWrote {typeFilename} of length {tableType.Value.Length}.");

            }
        }

        private static string FormatTypeString(string typeStr)
        {
            return typeStr.Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .ToUpper();
        }

        public override string ToString() => $"{TablesDir}\n{string.Join(NewLine, TableSqlFiles.Select(sqlFile => Tab + sqlFile))}";
    }
}
