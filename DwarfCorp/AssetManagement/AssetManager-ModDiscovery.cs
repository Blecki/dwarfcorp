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
    public static partial class AssetManager
    {
        public static List<ModMetaData> DiscoverMods()
        {
            // Todo: Restore steam functionality
            //var subscribed = AssetManagement.Steam.Steam.GetSubscribedMods();
            List<ModMetaData> subscribedMods = new List<ModMetaData>();
            //foreach(var m in subscribed)
            //{
            //    ulong size;
            //    string folder;
            //    uint folderSize = 2048;
            //    uint timestamp;
            //    if (Steamworks.SteamUGC.GetItemInstallInfo((Steamworks.PublishedFileId_t)m.m_PublishedFileId, out size, 
            //        out folder, folderSize, out timestamp))
            //    {
            //        var mod = GetMod(folder, ModSource.SteamDirectory);
            //        if (mod != null)
            //        {
            //            subscribedMods.Add(mod);
            //        }
            //    };

            //}

            return subscribedMods.Concat(EnumerateMods(GameSettings.Current.LocalModDirectory, ModSource.LocalDirectory)).ToList();
        }

        private static ModMetaData GetMod(string dir, ModSource Source)
        {
            try
            {
                var metaDataPath = dir + Path.DirectorySeparatorChar + "meta.json";
                var metaData = FileUtils.LoadJsonFromAbsolutePath<ModMetaData>(metaDataPath);
                metaData.Directory = dir;
                metaData.Source = Source;

                if (dir.StartsWith(GameSettings.Current.SteamModDirectory))
                    metaData.SteamID = ulong.Parse(global::System.IO.Path.GetFileName(dir));
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
                var metaData = GetMod(dir, Source);
                if (metaData != null)
                    r.Add(metaData);
            }

            return r;    
        }

    }

}