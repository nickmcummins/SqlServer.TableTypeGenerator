using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Data;

namespace DotNet.GenerateTableTypes
{
    public class TableTypeGenerationOptions
    {
        public string TypeName { get; }
        public IEnumerable<string> AddColumnsFromTables { get; }
        public IDictionary<string, DataTypeSpec> AdditionalColumns { get; }

        public TableTypeGenerationOptions(string typename, IEnumerable<string> addColumnsFromTables, IDictionary<string, DataTypeSpec> additionalColumns)
        {
            TypeName = typename;
            AddColumnsFromTables = addColumnsFromTables;
            AdditionalColumns = additionalColumns;
        }


        public TableTypeGenerationOptions(string optionsStr) : this(optionsStr.Split("=")[0], optionsStr.Split("=")[1].Split(";")[0].Split(","), optionsStr.Split("=")[1].Split(";").ToDictionary(colsString => colsString.Split(":")[0], colsString => TableTypeGenerationOptionsBinder.ToDataTypeSpec(colsString.Split(":")[1]))) { }
    }


    public class TableTypeGenerationOptionsBinder : BinderBase<IEnumerable<TableTypeGenerationOptions>>
    {
        private readonly IEnumerable<Option<string>> _typesGenerationOptions;

        public TableTypeGenerationOptionsBinder(IEnumerable<Option<string>> typesGenerationOptions)
        {
            _typesGenerationOptions = typesGenerationOptions;
        }

        protected override IEnumerable<TableTypeGenerationOptions> GetBoundValue(BindingContext bindingContext)
        {
            return _typesGenerationOptions.Select(tto => bindingContext.ParseResult.GetValueForOption<string>(tto)).Select(ttos => new TableTypeGenerationOptions(ttos));
        }

        public static DataTypeSpec ToDataTypeSpec(string datatypeStr)
        {
            switch (datatypeStr.ToLower())
            {
                case "varchar":
                    return DataTypeSpec.VarChar;
                default:
                    return null; 
            }
        }
    }
}
