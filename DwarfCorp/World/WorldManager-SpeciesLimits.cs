using System;
using System.Collections.Generic;

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
            if (PersistentData.SpeciesCounts.ContainsKey(Species.Name))
                PersistentData.SpeciesCounts[Species.Name] = Math.Max(0, PersistentData.SpeciesCounts[Species.Name] - 1);
        }

        public void DisplaySpeciesCountsInMetrics()
        {
            foreach (var species in PersistentData.SpeciesCounts)
                PerformanceMonitor.SetMetric(species.Key, species.Value);
        }

        public bool CanSpawnWithoutExceedingSpeciesLimit(CreatureSpecies Species)
        {
            if (!PersistentData.SpeciesCounts.ContainsKey(Species.Name))
                return true;
            var effectiveLimit = Math.Round(Species.SpeciesLimit * GameSettings.Default.SpeciesLimitAdjust);
            return PersistentData.SpeciesCounts[Species.Name] < effectiveLimit;
        }
    }
}
