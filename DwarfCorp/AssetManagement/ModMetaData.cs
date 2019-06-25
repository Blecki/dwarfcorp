using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public enum ModSource
    {
        LocalDirectory,
        SteamDirectory,
        SteamSubscribedButNotInstalled,
    }

    public class ModMetaData
    {
        public string Name;
        public string Description;
        public string PreviewURL;
        public List<string> Tags;
        public string ChangeNote;
        public ulong SteamID;

        public bool NeedsUpdateFromSteam = false;

        [JsonIgnore]
        public ModSource Source;

        [JsonIgnore]
        public String Directory;

        public void Save()
        {
            var metaDataPath = Directory + global::System.IO.Path.DirectorySeparatorChar + "meta.json";
            FileUtils.SaveJSON(this, metaDataPath);
        }

        public String IdentifierString
        {
            get
            {
                if (Source == ModSource.LocalDirectory)
                    return "/" + Directory;
                else
                    return SteamID.ToString();
            }
        }
    }
}
