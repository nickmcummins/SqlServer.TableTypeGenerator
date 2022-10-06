using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static SqlServer.TableTypeGenerator.Constants;

namespace SqlServer.TableTypeGenerator
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
        public static string ToString<T>(this IEnumerable<T> collection) where T : notnull
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

        public static bool MatchesAnyPattern(this string s, IEnumerable<string> patternStrs)
        {
            if (patternStrs == null)
            {
                return true;
            }

            foreach (var patternStr in patternStrs)
            {
                bool matches;
                if (patternStr.EndsWith("*"))
                {
                    matches = s.StartsWith(patternStr.Replace("*", ""));
                }
                else
                {
                    matches = s == patternStr;
                }

                if (matches) return true;
            }
            return false;
        }

        public static string TruncateAtSubsting(this string str, string substring)
        {
            return str.Substring(0, str.IndexOf(substring));
        }
    }

    public static class Constants
    {
        public const string Tab = "\t";
        public const string Comma = ",";
        public const string NewLine = "\n";
        public const string Space = " ";
    }
}
