using System;
using System.Diagnostics;
using System.Text;

namespace TumblerApp.Util
{
    internal class Log
    {
        /// <summary> Shorthand for System.Diagnostics.Debug.WriteLine </summary>
        public static void d(string s, params string[] tags) { AtLevel("DEBUG", s, tags); }
        public static void d(object o, params string[] tags) { d(o.ToString(), tags); }

        /// <summary>INFO log message</summary>
        public static void i(string s, params string[] tags) { AtLevel("INFO", s, tags); }

        /// <summary>WARN log message</summary>
        public static void w(string s, params string[] tags) { AtLevel("WARN", s, tags); }

        /// <summary>ERROR log message</summary>
        public static void e(string s, params string[] tags) { AtLevel("ERROR", s, tags); }
        public static void e(Exception exception, params string[] tags) { e(exception.ToString(), tags); }

        public static string AtLevel(string logLevel, string s, params string[] tags)
        {
            var logMsg = FormatLog(s, logLevel, tags);
            Debug.WriteLine(logMsg);
            return logMsg;
        }

        public static string FormatLog(string s, string logLevel, params string[] tags)
        {
            var bld = new StringBuilder();
            AppendTag(bld, $"{DateTime.Now:HH:mm:ss.fffff}");
            AppendTag(bld, logLevel);

            foreach (string tag in tags) AppendTag(bld, tag);
            return $"{bld} {s}";
        }
        internal static string CreateTags(params string[] tags)
        {
            if (tags == null) return string.Empty;

            var bld = new StringBuilder();
            foreach (string tag in tags) AppendTag(bld, tag);
            return bld.ToString();
        }
        internal static void AppendTag(StringBuilder bld, string tag) { bld.Append($"[{tag}]"); }

    }
}
