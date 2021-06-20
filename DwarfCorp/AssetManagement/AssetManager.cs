using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;

namespace DwarfCorp
{
    public class AssetException : Exception
    {
        public AssetException(String Message) : base(Message)
        {

        }
    }

    /// <summary>
    /// This class exists to provide an abstract interface between asset tags and textures. 
    /// Technically, the ContentManager already does this for XNA, but ContentManager is missing a
    /// couple of important functions: modability, and storing the *inverse* lookup between tag
    /// and texture. Additionally, the TextureManager provides an interface to directly load
    /// resources from the disk (rather than going through XNAs content system)
    /// </summary>
    public static partial class AssetManager
    {
        private static Dictionary<String, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static ContentManager Content { get { return GameState.Game.Content; } }
        private static GraphicsDevice Graphics {  get { return GameState.Game.GraphicsDevice; } }

        /// <summary>
        /// Assemblies created from loaded mods. Once discovered, this list will not change unless the game is reloaded.
        /// </summary>
        private static List<Tuple<ModMetaData,Assembly>> Assemblies = new List<Tuple<ModMetaData,Assembly>>();
        private static List<ModMetaData> DirectorySearchList;

        public static void ResetCache()
        {
            TextureCache.Clear();
        }

        public static void Initialize(ContentManager Content, GraphicsDevice Graphics, GameSettings.Settings Settings)
        {
            var installedMods = DiscoverMods();

            // Remove any mods that are no longer installed from the enabled mods list.
            Settings.EnabledMods = Settings.EnabledMods.Where(m => installedMods.Any(item => item.IdentifierString == m)).ToList();

            // Select mods in the order they are stored in enabled mods. Once this is set it is not changed.
            DirectorySearchList = Settings.EnabledMods.Select(m => installedMods.First(item => item.IdentifierString == m)).ToList();
            
            DirectorySearchList.Reverse();
            DirectorySearchList.Add(new ModMetaData
            {
                Name = "BaseContent",
                Directory = "Content",
            });

            Assemblies.Clear();
            Assemblies.Add(Tuple.Create(new ModMetaData
            {
                Name = "BaseContent",
                Directory = "Content",
            }, Assembly.GetExecutingAssembly()));

            // Compile any code files in the enabled mods.
            foreach (var mod in DirectorySearchList)
                if (global::System.IO.Directory.Exists(mod.Directory))
                {
                    var csFiles = Directory.EnumerateFiles(mod.Directory).Where(s => Path.GetExtension(s) == ".cs");
                    if (csFiles.Count() > 0)
                    {
                        var assembly = ModCompiler.CompileCode(csFiles);
                        if (assembly != null)
                            Assemblies.Add(Tuple.Create(mod, assembly));
                    }
                }
            TextureCache.Clear();
        }

        public static IEnumerable<Tuple<ModMetaData,Assembly>> EnumerateLoadedModAssemblies()
        {
            return Assemblies;
        }

        public static ModMetaData GetSourceModOfType(Type T)
        {
            foreach (var mod in Assemblies)
            {
                if (T.Assembly == mod.Item2)
                    return mod.Item1;
            }

            return new ModMetaData
            {
                Name = "BaseContent",
                Directory = "Content",
            }; // The type wasn't from one of the loaded mods.
        }

        public static Type GetTypeFromMod(String T, String Assembly)
        {
            foreach (var mod in Assemblies)
                if (mod.Item1.IdentifierString == Assembly)
                {
                    var type = mod.Item2.GetType(T);
                    if (type != null) return type;
                }

            var r = Type.GetType(T, true);
            if (r == null)
                throw new Exception("Unresolved type");
            return r;

            //throw new AssetException("Tried to load type from mod that is not installed or enabled");
        }

        private static bool CheckMethod(MethodInfo Method, Type ReturnType, Type[] ArgumentTypes)
        {
            if (!Method.IsStatic) return false;
            if (Method.ReturnType != ReturnType) return false;

            var parameters = Method.GetParameters();
            if (parameters.Length != ArgumentTypes.Length) return false;
            for (var i = 0; i < parameters.Length; ++i)
                if (parameters[i].ParameterType != ArgumentTypes[i]) return false;

            return true;
        }

        public static IEnumerable<MethodInfo> EnumerateModHooks(Type AttributeType, Type ReturnType, Type[] ArgumentTypes)
        {
            foreach (var assembly in EnumerateLoadedModAssemblies())
            {
                foreach (var type in assembly.Item2.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a.GetType() == AttributeType);
                        if (attribute == null) continue;
                        if (CheckMethod(method, ReturnType, ArgumentTypes))
                            yield return method;
                    }
                }
            }
        }

        public static string ReverseLookup(Texture2D Texture)
        {
            var r = TextureCache.Where(p => p.Value == Texture).Select(p => p.Key).FirstOrDefault();
            if (r == null) return "";
            return r;
        }

        public static String ResolveContentPath(String _Asset, params string[] AlternateExtensions)
        {
            string Asset = FileUtils.NormalizePath(_Asset);
            var extensionList = new List<String>(AlternateExtensions);
            if (extensionList.Count != 0)
                extensionList.Add(".xnb");
            else
                extensionList.Add("");

            foreach (var mod in DirectorySearchList)
            {
                foreach (var extension in extensionList)
                {
                    if (File.Exists(mod.Directory + Path.DirectorySeparatorChar + Asset + extension))
                        return mod.Directory + Path.DirectorySeparatorChar + Asset + extension;
                }
            }

            return "Content" + Path.DirectorySeparatorChar + Asset;
        }

        /// <summary>
        /// Enumerates the relative paths of all mods (including base content) that include the content.
        /// </summary>
        /// <param name="_AssetPath"></param>
        /// <returns></returns>
        public static IEnumerable<String> EnumerateMatchingPaths(String _AssetPath)
        {
            string AssetPath = FileUtils.NormalizePath(_AssetPath);

            foreach (var mod in DirectorySearchList)
            {
                var resolvedAssetPath = mod.Directory + Path.DirectorySeparatorChar + AssetPath;
                if (File.Exists(resolvedAssetPath))
                    yield return resolvedAssetPath;
            }
        }

        private static IEnumerable<String> EnumerateDirectory(String Path)
        {
            foreach (var file in Directory.EnumerateFiles(Path))
                yield return file;
            foreach (var directory in Directory.EnumerateDirectories(Path))
                foreach (var file in EnumerateDirectory(directory))
                    yield return file;
        }

        public static IEnumerable<String> EnumerateAllFiles(String BasePath)
        {
            string basePath = FileUtils.NormalizePath(BasePath);

            foreach (var mod in DirectorySearchList)
            {
                var directoryPath = mod.Directory + Path.DirectorySeparatorChar + basePath;
                if (!Directory.Exists(directoryPath)) continue;
                foreach (var file in EnumerateDirectory(directoryPath))
                    yield return file;
            }
        }

        public static bool DoesTextureExist(string _asset)
        {
            string asset = FileUtils.NormalizePath(_asset);
            if (asset == null)
            {
                return false;
            }

            if (TextureCache.ContainsKey(asset))
            {
                return true;
            }

            try
            {
                var filename = ResolveContentPath(asset, ".png");
                return File.Exists(filename);
            }
            catch (Exception exception)
            {
                return false;
            }
        }

        public static Texture2D GetContentTexture(string _asset)
        {
            if (String.IsNullOrEmpty(_asset))
            {
                DwarfGame.LogSentryBreadcrumb("AssetManager", "Attempt to load texture asset from empty string.", SharpRaven.Data.BreadcrumbLevel.Warning);
                return Content.Load<Texture2D>("Content/newgui/error");
            }

#if DEBUG
            if (!DwarfGame.IsMainThread)
            {
                // This completely breaks loading...
                //throw new InvalidOperationException("Can't load an asset outside of the main thread.");
            }
#endif
            string asset = FileUtils.NormalizePath(_asset);
            if (asset == null)
            {
                DwarfGame.LogSentryBreadcrumb("AssetManager", string.Format("Asset {0} was null.", _asset), SharpRaven.Data.BreadcrumbLevel.Warning);
                var r = Content.Load<Texture2D>(ContentPaths.Error);
                return r;
            }

            if (TextureCache.ContainsKey(asset))
            {
                var existing = TextureCache[asset];
                if (existing != null && !existing.IsDisposed && existing.GraphicsDevice != null && !existing.GraphicsDevice.IsDisposed)
                    return existing;
                else
                {
                    DwarfGame.LogSentryBreadcrumb("AssetManager", string.Format("Asset {0} was invalid.", asset), SharpRaven.Data.BreadcrumbLevel.Warning);
                    TextureCache.Remove(asset);
                }
            }

            try
            {
                var filename = ResolveContentPath(asset, ".png", ".bmp");
                if (Path.GetExtension(filename) == ".xnb")
                {
                    var toReturn = Content.Load<Texture2D>(filename.Substring(0, filename.Length - 4));
                    TextureCache[asset] = toReturn;
                    return toReturn;
                }
                else
                {
                    var toReturn = LoadUnbuiltTextureFromAbsolutePath(filename);
                    TextureCache[asset] = toReturn;
                    return toReturn;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                try
                {
                    DwarfGame.LogSentryBreadcrumb("AssetManager", string.Format("Failed to load asset {0} : {1}", asset, exception.ToString()), SharpRaven.Data.BreadcrumbLevel.Warning);
                    var r = Content.Load<Texture2D>(ContentPaths.Error);
                    TextureCache[asset] = r;
                    return r;
                }
                catch (Exception innerException)
                {
                    DwarfGame.LogSentryBreadcrumb("AssetManager", string.Format("Everything is broken! {0}", innerException.ToString()), SharpRaven.Data.BreadcrumbLevel.Error);
                    return null;
                }
            }
        }

        public static Texture2D RawLoadTexture(string filename)
        {
            try
            {
                if (Path.GetExtension(filename) == ".xnb")
                    return Content.Load<Texture2D>(filename.Substring(0, filename.Length - 4));
                else
                    return LoadUnbuiltTextureFromAbsolutePath(filename);
            }
            catch (ContentLoadException)
            {
                return null;
            }
        }

        public static Texture2D LoadUnbuiltTextureFromAbsolutePath(string _file)
        {
            try
            {
                string file = FileUtils.NormalizePath(_file);
                using (var stream = new FileStream(file, FileMode.Open))
                {
                    if (!stream.CanRead)
                    {
                        Console.Out.WriteLine("Failed to read {0}, stream cannot be read.", file);
                        return null;
                    }

                    try
                    {
                        return Texture2D.FromStream(GameState.Game.GraphicsDevice, stream);
                    }
                    catch (Exception exception)
                    {
                        Console.Out.Write("Failed to load texture {0}: {1}", file, exception.ToString());
                        return null;
                    }

                }
            }
            catch (Exception exception)
            {
                Console.Out.Write("Failed to load texture {0}", exception.ToString());
                return null;
            }
        }

        public static RawPrimitive GetContentMesh(string _asset)
        {
            if (String.IsNullOrEmpty(_asset))
            {
                DwarfGame.LogSentryBreadcrumb("AssetManager", "Attempt to load mesh asset from empty string.", SharpRaven.Data.BreadcrumbLevel.Warning);
                return null;
            }

            string asset = FileUtils.NormalizePath(_asset);
            if (asset == null)
            {
                DwarfGame.LogSentryBreadcrumb("AssetManager", string.Format("Asset {0} was null.", _asset), SharpRaven.Data.BreadcrumbLevel.Warning);
                return null;
            }

            try
            {
                var filename = ResolveContentPath(asset, ".obj");
                if (Path.GetExtension(filename) == ".xnb")
                {
                    var toReturn = Content.Load<ModelMesh>(filename.Substring(0, filename.Length - 4));

                    //TextureCache[asset] = toReturn;
                    //return toReturn;
                    // Todo: Convert model mesh to raw prim?
                    return null;
                }
                else if (Path.GetExtension(filename) == ".obj")
                {
                    return ObjLoader.LoadObject(File.ReadAllLines(FileUtils.NormalizePath(filename)));
                }
                else
                    return null;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                try
                {
                    DwarfGame.LogSentryBreadcrumb("AssetManager", string.Format("Failed to load asset {0} : {1}", asset, exception.ToString()), SharpRaven.Data.BreadcrumbLevel.Warning);
                    return null;
                }
                catch (Exception innerException)
                {
                    DwarfGame.LogSentryBreadcrumb("AssetManager", string.Format("Everything is broken! {0}", innerException.ToString()), SharpRaven.Data.BreadcrumbLevel.Error);
                    return null;
                }
            }
        }

    }

}