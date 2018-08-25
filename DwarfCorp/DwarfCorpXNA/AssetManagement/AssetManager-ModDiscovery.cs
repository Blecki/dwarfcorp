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
    public partial class AssetManager
    {
        public static List<ModMetaData> DiscoverMods()
        {
            var subscribed = AssetManagement.Steam.Steam.GetSubscribedMods();
            List<ModMetaData> subscribedMods = new List<ModMetaData>();
            foreach(var m in subscribed)
            {
                ulong size;
                string folder;
                uint folderSize = 2048;
                uint timestamp;
                if (Steamworks.SteamUGC.GetItemInstallInfo((Steamworks.PublishedFileId_t)m.m_PublishedFileId, out size, 
                    out folder, folderSize, out timestamp))
                {
                    var mod = GetMod(folder, ModSource.SteamDirectory);
                    if (mod != null)
                    {
                        subscribedMods.Add(mod);
                    }
                };

            }
            return subscribedMods.Concat(EnumerateMods(GameSettings.Default.LocalModDirectory, ModSource.LocalDirectory)).ToList();
        }

        private static ModMetaData GetMod(string dir, ModSource Source)
        {
            try
            {
                var metaDataPath = dir + ProgramData.DirChar + "meta.json";
                var metaData = FileUtils.LoadJsonFromAbsolutePath<ModMetaData>(metaDataPath);
                metaData.Directory = dir;
                metaData.Source = Source;

                if (dir.StartsWith(GameSettings.Default.SteamModDirectory))
                    metaData.SteamID = ulong.Parse(System.IO.Path.GetFileName(dir));
                return metaData;
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid mod: {0} {1}", dir, e.Message);
                return null;
            }
        }

        private static List<ModMetaData> EnumerateMods(String Path, ModSource Source)
        {
            var r = new List<ModMetaData>();

            if (!Directory.Exists(Path))
                return r;

            foreach (var dir in Directory.EnumerateDirectories(Path))
            {
                var metaData = GetMod(Path, Source);
                if (metaData != null)
                    r.Add(metaData);
            }

            return r;    
        }

    }

}