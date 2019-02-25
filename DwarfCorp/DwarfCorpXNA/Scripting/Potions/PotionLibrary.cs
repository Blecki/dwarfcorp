using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public static class PotionLibrary
    {
        public static Dictionary<string, Potion> Potions = null;

        public static void Initialize()
        {
            // Todo: Embedd potion object into item (or resource-item? Why is it in one and not the other) and do away with this library entirely.
            Potions = new Dictionary<string, Potion>();
            foreach (var potion in FileUtils.LoadJsonListFromMultipleSources<Potion>(ContentPaths.potions, null, p => p.Name))
                Potions.Add(potion.Name, potion);
        }
    }

}
