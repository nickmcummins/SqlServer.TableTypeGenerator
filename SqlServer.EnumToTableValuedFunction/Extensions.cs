using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text.Json;

namespace SqlServer.EnumToTableValuedFunction
{
    public static class Extensions
    {
        private static readonly string NewLine = "\n\n";
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> ts, Func<T, IEnumerable<T>> childFunc)
        {
            var leaves = new List<T>();
            foreach (var t in ts)
            {
                leaves.AddRange(t.Flatten(childFunc));
            }
            return leaves;
        }

        public static IEnumerable<T> Flatten<T>(this T t, Func<T, IEnumerable<T>> childFunc)
        {
            var leaves = new List<T>();
            var children = childFunc.Invoke(t);
            if (children != null)
            {
                foreach (var childT in children)
                {
                    leaves.AddRange(childT.Flatten(childFunc));
                }
            }
            leaves.Add(t);
            return leaves;
        }

        public static string ToJson<T>(this T obj)
        {
            return System.Text.Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes<T>(obj));

        }

        public static TOutput ConvertTo<TOutput>(this object obj)
        {
            return JsonSerializer.Deserialize<TOutput>(JsonSerializer.SerializeToUtf8Bytes(obj));
        }

        public static dynamic Cast(dynamic obj, Type castTo)
        {
            return Convert.ChangeType(obj, castTo);
        }

        public static IEnumerable<object> GetMembers(this object node)
        {
            PropertyInfo membersProperty = null;
            node.GetType().GetProperties().ToDictionary(p => p.Name).TryGetValue("Members", out membersProperty); 
            if (membersProperty != null)
            {
                membersProperty.GetType();
                return (IEnumerable<object>)membersProperty.GetValue(node);
            }
            else
            {
                return Enumerable.Empty<object>();
            }
        }

        public static TProperty GetProperty<TObject, TProperty>(this TObject obj, string propertyName)
        {
            var property = (typeof(TObject) != typeof(object) ? typeof(TObject) : obj.GetType()).GetProperties().FirstOrDefault(property => property.Name == propertyName);
            if (property == null)
            {
                var objDict = obj.ConvertTo<IDictionary<string, object>>();
                return (TProperty)objDict[propertyName];
            }
            return (TProperty)property.GetValue(obj);
        }
        public static string GetProperty(this object obj, string propertyName) => obj.GetProperty<object, string>(propertyName);
        public static string GetProperty<TObject>(this TObject obj, string propertyName) => obj.GetProperty<TObject, string>(propertyName);

        public static IDictionary<string, EnumMemberDeclarationSyntax> GetEnumMembers(this EnumDeclarationSyntax enumDeclaration)
        {
            return enumDeclaration.Members.Where(member => member.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.EnumMemberDeclaration)
                .ToDictionary(member => member.GetProperty<EnumMemberDeclarationSyntax, EnumMemberDeclarationSyntax>("UnderlyingNode").Identifier.Text);
        }

        public static string LowercaseFirst(this string s)=> string.Concat($"{s[0]}".ToLower(), s.AsSpan(1));

        public static string ToIfSetSql(string enumName, KeyValuePair<string, string> enumMember)
        {
            return
$"""
    IF (@{enumName.LowercaseFirst()}Id = {enumMember.Value})
    BEGIN
        SET @{enumName.LowercaseFirst()} = '{enumMember.Key}'
    END
""";
        }


        public static string ToTableValuedFunction(string enumName, IDictionary<string, string> enumMembers) => $"""
CREATE FUNCTION [sds].[To{enumName}]
(
    @{enumName.LowercaseFirst()}Id INT
)
RETURNS VARCHAR({enumMembers.Keys.Max(enumMember => enumMember.Length)})
AS
BEGIN
    DECLARE @{enumName.LowercaseFirst()} VARCHAR({enumMembers.Keys.Max(enumMember => enumMember.Length)})

{string.Join(NewLine, enumMembers.Select(enumMember => ToIfSetSql(enumName, enumMember)))}

  RETURN @{enumName.LowercaseFirst()}
END
""";
    }
}
