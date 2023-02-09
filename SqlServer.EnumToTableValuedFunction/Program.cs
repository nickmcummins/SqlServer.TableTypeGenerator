using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqlServer.EnumToTableValuedFunction;
using static SqlServer.EnumToTableValuedFunction.Extensions;

var enumFilename = args[0];
var enumName = args[1];
var csharpSyntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(File.ReadAllText(enumFilename));
CompilationUnitSyntax root = (CompilationUnitSyntax)csharpSyntaxTree.GetRoot();
var members = (root.Members as IEnumerable<object>).Flatten(member => member.GetMembers());
var enumDefs = members.Where(member => member is EnumDeclarationSyntax enumDecl)
    .ToDictionary(
        enumDef => ((EnumDeclarationSyntax)enumDef).Identifier.Text,
        enumDef => ((EnumDeclarationSyntax)enumDef).Members.ToDictionary(enumMember => enumMember.Identifier.Text, enumMember => enumMember.ToString().Split(" ").Last().Trim())
);
var enumMembers = enumDefs[enumName];

var tvf = ToTableValuedFunction(enumName, enumMembers);
Console.Out.WriteLine(tvf);