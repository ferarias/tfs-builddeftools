using System;
using System.Collections.Generic;
using System.Linq;

namespace TfsBuildDefinitionsCommon
{
    public static class Helpers
    {
        public static string GetBuildName(string name, string folder)
        {
            var buildName = name;
            if (!buildName.EndsWith("." + folder))
            {
                buildName = buildName + "." + folder;
            }
            return buildName;
        }

        

        public static bool ObjectsAreEquivalent(object a, object b)
        {
            if (!a.GetType().IsArray || !b.GetType().IsArray) return a.Equals(b);
            var list1 = a as Array;
            var list2 = b as Array;
            if (list1 == null || list2 == null) return false;
            if (list1.Length != list2.Length) return false;

            for (var i = 0; i < list1.Length; i++)
                if (!ObjectsAreEquivalent(list1.GetValue(i), list2.GetValue(i)))
                    return false;
            return true;
        }

        

        public static string ObjectToString(Object obj)
        {
            if (obj == null) return "<NULL>";
            var objects = obj as IEnumerable<object>;
            return objects != null ? String.Join(",", objects.Select(x => x.ToString()).ToArray()) : obj.ToString();
        }
    }
}
