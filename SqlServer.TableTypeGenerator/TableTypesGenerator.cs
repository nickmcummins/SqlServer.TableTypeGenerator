using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static SqlServer.TableTypeGenerator.Constants;

namespace SqlServer.TableTypeGenerator
{
    public class TableTypesGenerator
    {
        private string TablesDir { get; }
        private IEnumerable<string> TableSqlFiles { get; }
        public TableTypeGenerationOptions TypeGenerationOptions { get; }
        public IDictionary<string, IEnumerable<SqlColumnDefinition>> TableDefinitions { get; }
        public IDictionary<string, string> TableTypeDefinitions { get; }


        public TableTypesGenerator(string tablesDir, TableTypeGenerationOptions tableTypesGenerationOptions)
        {
            TablesDir = tablesDir;
            TypeGenerationOptions = tableTypesGenerationOptions;
            TableSqlFiles = Utils.GetDirectories(tablesDir)
                .SelectMany(dir => Directory.GetFiles(dir).Where(filename => !filename.EndsWith("_History.sql")));
            TableDefinitions = GetCreateTableStatements(TableSqlFiles);
            TableTypeDefinitions = TableSqlFiles
                .Where(tableSqlFile => TableDefinitions.ContainsKey(Path.GetFileNameWithoutExtension(tableSqlFile)))
                .ToDictionary(
                    tableSqlFile => tableSqlFile, 
                    tableSqlFile => GetCreateTypeSql(Path.GetFileNameWithoutExtension(tableSqlFile)));
        }

        private IDictionary<string, IEnumerable<SqlColumnDefinition>> GetCreateTableStatements(IEnumerable<string> tableSqlFiles)
        {
            var createTables = new Dictionary<string, IEnumerable<SqlColumnDefinition>>();

            foreach (var tableSqlFile in tableSqlFiles.Where(tableSqlFile => Path.GetFileNameWithoutExtension(tableSqlFile).MatchesAnyPattern(TypeGenerationOptions.TablesFilter) || TypeGenerationOptions.ReferencedTableDependencies.Contains(Path.GetFileNameWithoutExtension(tableSqlFile))))
            {
                var parsedSql = Parser.Parse(File.ReadAllText(tableSqlFile)).Script.Children
                    .Flatten(child => child.Children)
                    .Where(child => child is SqlCreateTableStatement && child is not null)
                    .Select(child => (SqlCreateTableStatement)child);
                var createTableStatement = parsedSql.FirstOrDefault();
                if (createTableStatement != null)
                {
                    createTables[Path.GetFileNameWithoutExtension(tableSqlFile)] = parsedSql.First().Definition.ColumnDefinitions;
                }
            }

            return createTables;
        }

        private string GetCreateTypeSql(string typename)
        {
            var columns = TableDefinitions[typename].ToList();
            var columnStrs = columns
                .Select(colDef => Tab + colDef.Name + Space + FormatTypeString(colDef.DataType.Sql))
                .ToList();

            int i = 0;
            var colNames = new HashSet<string>();
            foreach (var addColumn in TypeGenerationOptions.AdditionalColumns.TryGetValue(typename, out var addColumns) ? addColumns : Enumerable.Empty<string>())
            {
                if (!colNames.Contains(addColumn))
                {
                    columnStrs.Insert(i, Tab + addColumn.Replace(":", " "));
                    colNames.Add(addColumn);
                    i++;
                }
            }

            foreach (var table in TypeGenerationOptions.AddColumnsFromTables.TryGetValue(typename, out var tables) ? tables : Enumerable.Empty<string>())
            {
                columnStrs.AddRange(TableDefinitions[table]
                    .Where(colDef => !colNames.Contains(colDef.Name.Value))
                    .Select(colDef => Tab + colDef.Name + Space + FormatTypeString(colDef.DataType.Sql)));
            }

            if (TypeGenerationOptions.RenamedTypes.TryGetValue(typename, out var renamed))
            {
                typename = renamed;
            }
            return $"CREATE TYPE [clm].[{typename}Type] AS TABLE (\n{string.Join(Comma + NewLine, columnStrs)}\n)"; 
        }
        

        public void WriteFiles()
        {

            var typesDir = TablesDir.Replace("Tables", "GeneratedTypes");
            if (!Directory.Exists(typesDir))
            {
                Directory.CreateDirectory(typesDir);
            }
            foreach (var tableType in TableTypeDefinitions.Where(typeDef => !TypeGenerationOptions.ReferencedTableDependencies.Contains($"{Path.GetFileNameWithoutExtension(typeDef.Key)}")))
            {
                var typeName = Path.GetFileNameWithoutExtension(tableType.Key);
                var tablesFolder = Path.GetFileName(TablesDir);
                if (TypeGenerationOptions.RenamedTypes.TryGetValue(typeName, out var renamed))
                {
                    typeName = renamed;
                }
                var typeFilename = @$"{TablesDir.TruncateAtSubsting(tablesFolder)}\GeneratedTypes\{typeName}Type.sql";
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
