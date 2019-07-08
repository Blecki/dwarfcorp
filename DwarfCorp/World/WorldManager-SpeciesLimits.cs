using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using BloomPostprocess;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using Newtonsoft.Json;
using DwarfCorp.Events;

namespace DwarfCorp
{
    public partial class PersistentWorldData
    {
        public Dictionary<string, int> SpeciesCounts = new Dictionary<string, int>();
    }

    public partial class WorldManager : IDisposable
    {

        public void AddToSpeciesTracking(CreatureSpecies Species)
        {
            if (!PersistentData.SpeciesCounts.ContainsKey(Species.Name))
                PersistentData.SpeciesCounts.Add(Species.Name, 0);

            PersistentData.SpeciesCounts[Species.Name] += 1;
        }

        public void RemoveFromSpeciesTracking(CreatureSpecies Species)
        {
            if (!PersistentData.SpeciesCounts.ContainsKey(Species.Name))
                PersistentData.SpeciesCounts.Add(Species.Name, 0);
            else
                PersistentData.SpeciesCounts[Species.Name] += 1;
        }

        public int GetSpeciesPopulation(CreatureSpecies Species)
        {
            if (!PersistentData.SpeciesCounts.ContainsKey(Species.Name))
                return 0;
            return PersistentData.SpeciesCounts[Species.Name];
        }
    }
}
