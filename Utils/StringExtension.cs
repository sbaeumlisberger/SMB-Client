using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Utils
{
    public static class StringExtension
    {
        public static string ReplacePostfix(this string source, string postfix, string replacment)
        {
            if (source.EndsWith(postfix))
            {
                return source.Substring(0, source.Length - postfix.Length) + replacment;
            }
            return source;
        }

        public static string RemovePostfix(this string source, string postfix)
        {
            return source.ReplacePostfix(postfix, "");
        }

        public static string ReplacePrefix(this string source, string prefix, string replacment)
        {
            if (source.StartsWith(prefix))
            {
                return replacment + source.Substring(prefix.Length, source.Length - prefix.Length);
            }
            return source;
        }

        public static string RemovePrefix(this string source, string prefix)
        {
            return source.ReplacePrefix(prefix, "");
        }
    }
}
