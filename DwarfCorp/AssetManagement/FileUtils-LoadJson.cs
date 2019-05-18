using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Collections.Generic;

namespace DwarfCorp
{
    public static partial class FileUtils
    {
        public static T LoadJsonFromAbsolutePath<T>(string filePath, object context = null)
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

        public static T LoadJsonFromResolvedPath<T>(string filePath, object context = null)
        {
            using (var stream = new FileStream(AssetManager.ResolveContentPath(filePath), FileMode.Open))
            using (StreamReader reader = new StreamReader(stream))
            {
                using (JsonReader json = new JsonTextReader(reader))
                {
                    return GetStandardSerializer(context).Deserialize<T>(json);
                }
            }
        }

        /// <summary>
        /// Load a json list from all enabled mods, combining entries into one list. JSON must contain a List<T> as the top level element.</T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="AssetPath"></param>
        /// <param name="Context"></param>
        /// <param name="Name">Given a T, this func must return a unique name - this is used to compare items to see if they should be overriden by the mod.</param>
        /// <returns></returns>
        public static List<T> LoadJsonListFromMultipleSources<T>(String AssetPath, Object Context, Func<T,String> Name)
        {
            var result = new Dictionary<String, T>();

            foreach (var resolvedAssetPath in AssetManager.EnumerateMatchingPaths(AssetPath))
            {
                var list = LoadJsonFromAbsolutePath<List<T>>(resolvedAssetPath, Context);
                foreach (var item in list)
                {
                    var name = Name(item);
                    if (!result.ContainsKey(name))
                        result.Add(name, item);
                }
            }

            return new List<T>(result.Values);
        }

        public static List<T> LoadJsonListFromDirectory<T>(String DirectoryPath, Object Context, Func<T, String> Name)
        {
            var result = new Dictionary<String, T>();

            foreach (var resolvedAssetPath in AssetManager.EnumerateAllFiles(DirectoryPath))
            {
                try
                {
                    var item = LoadJsonFromAbsolutePath<T>(resolvedAssetPath, Context);
                    var name = Name(item);

                    if (!result.ContainsKey(name))
                        result.Add(name, item);
                }
                catch (Exception e)
                {
                    DwarfGame.LogSentryBreadcrumb("AssetManager", String.Format("Could not load json: {0} msg: {1}", resolvedAssetPath, e.Message), SharpRaven.Data.BreadcrumbLevel.Error);
                    Console.WriteLine("Error loading asset {0}: {1}", resolvedAssetPath, e.Message);
                }
            }

            return new List<T>(result.Values);
        }

        public static List<String> LoadConfigurationLinesFromMultipleSources(String AssetPath)
        {
            var result = new List<String>();

            foreach (var resolvedAssetPath in AssetManager.EnumerateMatchingPaths(AssetPath))
                result.AddRange(global::System.IO.File.ReadAllLines(resolvedAssetPath));

            return result;
        }
    }
}
