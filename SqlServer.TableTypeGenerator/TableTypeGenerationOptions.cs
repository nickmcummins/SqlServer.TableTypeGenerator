using Microsoft.SqlServer.Management.SqlParser.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace SqlServer.TableTypeGenerator
{
    public class TableTypeGenerationOptions
    {
        public IEnumerable<string> TablesFilter { get; }
        public IEnumerable<string> ReferencedTableDependencies { get; }
        public IDictionary<string, string[]> AddColumnsFromTables { get; }
        public IDictionary<string, string[]> AdditionalColumns { get; }
        public IDictionary<string, string> RenamedTypes { get; }

        public TableTypeGenerationOptions(IEnumerable<string> tablesFilter, IDictionary<string, string[]> additionalColumns, IDictionary<string, string[]> addColumnsFromTables, IDictionary<string, string> renamedTypes)
        {
            TablesFilter = tablesFilter;
            ReferencedTableDependencies = addColumnsFromTables.SelectMany(tableTRefs => tableTRefs.Value.Distinct().Where(tableRef => !tableRef.MatchesAnyPattern(TablesFilter)));
            AddColumnsFromTables = addColumnsFromTables;
            AdditionalColumns = additionalColumns;
            RenamedTypes = renamedTypes;
        }
    }
}
