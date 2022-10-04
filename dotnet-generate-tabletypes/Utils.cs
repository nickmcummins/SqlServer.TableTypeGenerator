using static DotNet.GenerateTableTypes.Constants;

namespace DotNet.GenerateTableTypes
{
    public static class Utils
    {

        public static IEnumerable<string> GetDirectories(string directory)
        {
            var dirs = new List<string>() { directory };
            foreach (var subdir in Directory.GetDirectories(directory))
            {
                dirs.AddRange(GetDirectories(subdir));
            }

            return dirs;
        }
    }

    public static class Extensions
    {
        public static string ToString<T>(this IEnumerable<T> collection)
        {
            return $"[{string.Join(Comma, collection.Select(item => item.ToString()))}]";
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> ts, Func<T, IEnumerable<T>> childFunc)
        {
            var leaves = new List<T>();
            foreach (var t in ts)
            {
                leaves.AddRange(t.Flatten<T>(childFunc));
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
    }

    public static class Constants
    {
        public const string Tab = "\t";
        public const string Comma = ",";
        public const string NewLine = "\n";
    }
}
