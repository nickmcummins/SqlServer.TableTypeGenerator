using Microsoft.Build.Framework;
using SqlServer.TableTypeGenerator;
using System.Linq;
using Task = Microsoft.Build.Utilities.Task;

namespace SqlServer.TableTypeGenerator.MSBuildTask
{
    public class GenerateTableTypes : Task
    {
        public string TablesFilter { get; set; }

        [Required]
        public string TablesDir { get; set; }
        [Required]
        public string ReferencedTableDependencies { get; set; }

        [Required]
        public string AddColumnsFromTables { get; set; }

        [Required]
        public string AdditionalColumns { get; set; }

        [Required]
        public string RenamedTypes { get; set; }

        public override bool Execute()
        {
            var additionalColumnsByType = AdditionalColumns.Split(';')
                .ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1].Split(','));

            var addFromTablesByType = AddColumnsFromTables.Split(';')
                .ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1].Split(','));
            var tables = TablesFilter.Split(',').ToList();
            var renamedTypes = RenamedTypes.Split(',').ToDictionary(rn => rn.Split('=')[0], rn => rn.Split('=')[1]);
            var ttGenerator = new TableTypesGenerator(TablesDir, new TableTypeGenerationOptions(tables, additionalColumnsByType, addFromTablesByType, renamedTypes));
            ttGenerator.WriteFiles();

            return true;
        }
    }
}
