using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facepunch.Steamworks;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public enum ModSource
    {
        LocalDirectory,
        SteamDirectory
    }

    public class ModMetaData
    {
        public string Name;
        public string Description;
        public string PreviewURL;
        public List<string> Tags;
        public string ChangeNote;
        public ulong SteamID;
        public Guid Guid;

        [JsonIgnore]
        public ModSource Source;

        [JsonIgnore]
        public String Directory;
    }
}
