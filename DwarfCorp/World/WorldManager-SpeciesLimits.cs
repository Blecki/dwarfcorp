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
    /// <summary>
    /// This is the main game state for actually playing the game.
    /// </summary>
    public partial class WorldManager : IDisposable
    {
        [JsonProperty] private Dictionary<string, int> SpeciesCounts = new Dictionary<string, int>();

        public void AddToSpeciesTracking(CreatureClass Class)
        {
            if (!SpeciesCounts.ContainsKey(Class.Name))
                SpeciesCounts.Add(Class.Name, 0);

            SpeciesCounts[Class.Name] += 1;
        }

        public void RemoveFromSpeciesTracking(CreatureClass Class)
        {
            if (!SpeciesCounts.ContainsKey(Class.Name))
                SpeciesCounts.Add(Class.Name, 0);
            else
                SpeciesCounts[Class.Name] += 1;
        }

        public int GetSpeciesPopulation(CreatureClass Class)
        {
            if (!SpeciesCounts.ContainsKey(Class.Name))
                return 0;
            return SpeciesCounts[Class.Name];
        }
    }
}
