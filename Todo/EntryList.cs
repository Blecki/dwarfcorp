using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.IO;

namespace TodoList
{
    public class EntryList
    {
        public UInt32 NextID = 1;
        public List<Entry> PreviousVersions = new List<Entry>();
        public Entry Root = new Entry();

        public static EntryList LoadFile(String File, bool PlanningMutation)
        {
            if (!System.IO.File.Exists(File))
                System.IO.File.WriteAllText(File, "{}");

            var r = LoadJsonFromAbsolutePath<EntryList>(File);

            if (PlanningMutation)
            {
                var copy = LoadFile(File, false);
                if (r.PreviousVersions.Count > 15)
                    r.PreviousVersions.RemoveAt(0);
                r.PreviousVersions.Add(copy.Root);
            }

            return r;
        }

        public static void SaveFile(String File, EntryList Root)
        {
            using (StreamWriter filestream = new StreamWriter(System.IO.File.Open(File, global::System.IO.FileMode.Truncate)))
            using (JsonWriter writer = new JsonTextWriter(filestream))
            {
                GetStandardSerializer(null).Serialize(writer, Root);
            }
        }

        private static T LoadJsonFromAbsolutePath<T>(string filePath, object context = null)
        {
            using (var stream = new FileStream(filePath, FileMode.Open))
            using (StreamReader reader = new StreamReader(stream))
            {
                using (JsonReader json = new JsonTextReader(reader))
                {
                    return GetStandardSerializer(context).Deserialize<T>(json);
                }
            }
        }

        private static JsonSerializer GetStandardSerializer(Object Context)
        {
            var serializer = new JsonSerializer
            {
                Context = new StreamingContext(StreamingContextStates.File, Context),
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            };

            return serializer;
        }
    }
}