using System.Collections;
using System.Text;

namespace TumblerApp.Util
{
    public static class StringUtil
    {
        public static string ToDebugString(this object o)
        {
            return ObjToString(o);
        }

        public static string ObjToString(object o)
        {
            if (o == null) return "null";
            if (o is IDictionary innerDict) return DicToString(innerDict);
            if (o is IList innerList) return ListToString(innerList);
            if (o is string) return $"\"{o}\"";
            return o.ToString();
        }

        private static string ListToString(IList list)
        {
            if (list == null) return "null";
            var bld = new StringBuilder("[");

            foreach (object item in list)
            {
                bld.Append(ObjToString(item) + ", ");
            }

            if (list.Count > 0) bld.Length -= 2;
            bld.Append("]");

            return bld.ToString();
        }

        private static string DicToString(IDictionary dict)
        {
            if (dict == null) return "null";

            var bld = new StringBuilder("{");
            foreach (object key in dict.Keys)
            {
                string strKey = key is string ? $"\"{key}\"" : key.ToString();
                bld.Append($"{strKey}: {ObjToString(dict[key])}, ");
            }

            if (dict.Keys.Count > 0) bld.Length -= 2;
            bld.Append("}");
            return bld.ToString();
        }
    }
}