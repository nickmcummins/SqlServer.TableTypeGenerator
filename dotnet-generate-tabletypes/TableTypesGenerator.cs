using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using static DotNet.GenerateTableTypes.Constants;

namespace DotNet.GenerateTableTypes
{
    public class TableTypesGenerator
    {
        private string TablesDir { get; }
        private IEnumerable<string> TableSqlFiles { get; }
        public IDictionary<string, TableTypeGenerationOptions> TypeGenerationOptions { get; }
        public IDictionary<string, IEnumerable<DataTypeSpec>> TableDefinitions { get; }
        public IDictionary<string, string> TableTypeDefinitions { get; }

        public TableTypesGenerator(string tablesDir, IEnumerable<string> tableTypesGenerationOptions) : this(tablesDir, tableTypesGenerationOptions.Select(tto => new TableTypeGenerationOptions(tto))) { }

        public TableTypesGenerator(string tablesDir, IEnumerable<TableTypeGenerationOptions> tableTypesGenerationOptions)
        {
            TablesDir = tablesDir;
            TypeGenerationOptions = tableTypesGenerationOptions.ToDictionary(tt => tt.TypeName);
            TableSqlFiles = Utils.GetDirectories(tablesDir)
                .SelectMany(dir => Directory.GetFiles(dir).Where(filename => !filename.EndsWith("_History.sql")));
            TableDefinitions = GetCreateTableStatements(TableSqlFiles);
            TableTypeDefinitions = TableSqlFiles
                .ToDictionary(
                    tableSqlFile => tableSqlFile, 
                    tableSqlFile => GetCreateTypeSql(Path.GetFileNameWithoutExtension(tableSqlFile)));
        }

        private static IDictionary<string, IEnumerable<DataTypeSpec>> GetCreateTableStatements(IEnumerable<string> tableSqlFiles)
        {
            var createTables = new Dictionary<string, IEnumerable<DataTypeSpec>>();
            foreach (var tableSqlFile in tableSqlFiles)
            {
                var parsedSql = Parser.Parse(File.ReadAllText(tableSqlFile)).Script.Children
                    .Flatten(child => child.Children)
                    .Where(child => child is SqlCreateTableStatement)
                    .Select(child => child as SqlCreateTableStatement);
                createTables[tableSqlFile] = parsedSql.First().Definition.ColumnDefinitions
                    .Select(colDef => colDef.DataType.DataType.GetTypeSpec());
            }

            return createTables;
        }

        private string GetCreateTypeSql(string typename)
        {
            var columns = TableDefinitions[typename].ToList();
            if (TypeGenerationOptions.TryGetValue(typename, out var typeOptions))
            {
                foreach (var additionalCol in typeOptions.AdditionalColumns.Values)
                {
                    columns.Insert(0, additionalCol);
                }
                foreach (var table in typeOptions.AddColumnsFromTables)
                {
                    columns.AddRange(TableDefinitions[table]);
                }
            }

            return $"CREATE TYPE [clm].[{typename}Type] AS TABLE (\n{string.Join(NewLine, columns.Select(colDef => Tab + colDef.Name + FormatTypeString(colDef.SqlDataType.ToString())))}\n)"; 
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
