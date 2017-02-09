using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum
{
    public static class HelperExtensions
    {
        public static void Upsert<A, B>(this Dictionary<A, B> Dict, A _a, B _b)
        {
            if (Dict.ContainsKey(_a)) Dict[_a] = _b;
            else Dict.Add(_a, _b);
        }

        public static void Add<T>(this List<T> List, params T[] objects)
        {
            List.AddRange(objects);
        }

        public static B ValueOrDefault<A, B>(this Dictionary<A, B> Dict, A Key)
        {
            if (Dict.ContainsKey(Key)) return Dict[Key];
            else return default(B);
        }

        public static B ValueOrDefault<A, B>(this Dictionary<A, Object> Dict, A Key, B Default)
        {
            if (Dict.ContainsKey(Key)) return (B)Dict[Key];
            else return Default;
        }

        public static B TypedValue<B>(this Dictionary<String, Object> Dict, String Key) where B : class
        {
            if (Dict.ContainsKey(Key)) return Dict[Key] as B;
            else return default(B);
        }

        public static Dictionary<String, Object> MakeDictionary(params Object[] Pairs)
        {
            var r = new Dictionary<String, Object>();
            for (int i = 0; (i + 1) < Pairs.Length; ++i)
                r.Upsert(Pairs[i].ToString().ToUpper(), Pairs[i + 1]);
            return r;
        }
    }
}

