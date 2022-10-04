using DotNet.GenerateTableTypes;

var tablesDir = args[0];
var tableTypeGenerator = new TableTypeGenerator(tablesDir);
tableTypeGenerator.WriteFiles();