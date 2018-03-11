// TextureManager.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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

    /// <summary>
    /// This class exists to provide an abstract interface between asset tags and textures. 
    /// Technically, the ContentManager already does this for XNA, but ContentManager is missing a
    /// couple of important functions: modability, and storing the *inverse* lookup between tag
    /// and texture. Additionally, the TextureManager provides an interface to directly load
    /// resources from the disk (rather than going through XNAs content system)
    /// </summary>
    public class AssetManager
    {
        private static Dictionary<String, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static ContentManager Content;
        private static GraphicsDevice Graphics;
        private static List<Assembly> Assemblies = new List<Assembly>();

        public static void Initialize(ContentManager Content, GraphicsDevice Graphics, GameSettings.Settings Settings)
        {
            AssetManager.Content = Content;
            AssetManager.Graphics = Graphics;

            Assemblies.Add(Assembly.GetExecutingAssembly());

            foreach (var mod in EnumerateModDirectories(Settings))
                if (System.IO.Directory.Exists(mod))
                    foreach (var file in System.IO.Directory.EnumerateFiles(mod))
                        if (System.IO.Path.GetExtension(file) == ".dll")
                            Assemblies.Add(Assembly.LoadFile(System.IO.Path.GetFullPath(file)));
        }

        public static IEnumerable<Assembly> EnumerateLoadedModAssemblies()
        {
            return Assemblies;
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
                foreach (var type in assembly.GetTypes())
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

        private static IEnumerable<String> EnumerateModDirectories(GameSettings.Settings Settings)
        {
            var searchList = Settings.EnabledMods.Select(m => "Mods" + ProgramData.DirChar + m).ToList();
            searchList.Reverse();
            searchList.Add("Content");
            return searchList;
        }

        public static string ReverseLookup(Texture2D Texture)
        {
            var r = TextureCache.Where(p => p.Value == Texture).Select(p => p.Key).FirstOrDefault();
            if (r == null) return "";
            return r;
        }

        public static String ResolveContentPath(String Asset, params string[] AlternateExtensions)
        {
            var extensionList = new List<String>(AlternateExtensions);
            if (extensionList.Count != 0)
                extensionList.Add(".xnb");
            else
                extensionList.Add("");

            foreach (var mod in EnumerateModDirectories(GameSettings.Default))
            {
                foreach (var extension in extensionList)
                {
                    if (File.Exists(mod + ProgramData.DirChar + Asset + extension))
                        return mod + ProgramData.DirChar + Asset + extension;
                }
            }

            return "Content" + ProgramData.DirChar + Asset;
        }

        /// <summary>
        /// Enumerates the relative paths of all mods (including base content) that include the content.
        /// </summary>
        /// <param name="Asset"></param>
        /// <returns></returns>
        public static IEnumerable<String> EnumerateMatchingPaths(String AssetPath)
        {
            var searchList = GameSettings.Default.EnabledMods.Select(m => "Mods" + ProgramData.DirChar + m).ToList();
            searchList.Reverse();
            searchList.Add("Content");

            foreach (var mod in searchList)
            {
                var resolvedAssetPath = mod + ProgramData.DirChar + AssetPath;
                if (File.Exists(resolvedAssetPath))
                    yield return resolvedAssetPath;
            }
        }
        
        public static Texture2D GetContentTexture(string asset)
        {
            if (TextureCache.ContainsKey(asset))
                return TextureCache[asset];

            try
            {
                var filename = ResolveContentPath(asset, ".png");
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
            catch (ContentLoadException exception)
            {
                Console.Error.WriteLine(exception.ToString());
                var r = Content.Load<Texture2D>(ContentPaths.Error);
                TextureCache[asset] = r;
                return r;
            }

        }

        public static Texture2D LoadUnbuiltTextureFromAbsolutePath(string file)
        {
            using(var stream = new FileStream(file, FileMode.Open))
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
    }

}