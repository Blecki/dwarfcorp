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

        public void AddToSpeciesTracking(MaybeNull<CreatureSpecies> Species)
        {
            if (Species.HasValue(out var species))
            {
                if (!PersistentData.SpeciesCounts.ContainsKey(species.Name))
                    PersistentData.SpeciesCounts.Add(species.Name, 0);

                PersistentData.SpeciesCounts[species.Name] += 1;
            }
        }

        public void RemoveFromSpeciesTracking(MaybeNull<CreatureSpecies> Species)
        {
            if (Species.HasValue(out var species) && PersistentData.SpeciesCounts.ContainsKey(species.Name))
                PersistentData.SpeciesCounts[species.Name] = Math.Max(0, PersistentData.SpeciesCounts[species.Name] - 1);
        }

        public void DisplaySpeciesCountsInMetrics()
        {
            foreach (var species in PersistentData.SpeciesCounts)
                PerformanceMonitor.SetMetric(species.Key, species.Value);
        }

        public bool CanSpawnWithoutExceedingSpeciesLimit(MaybeNull<CreatureSpecies> Species)
        {
            if (Species.HasValue(out var species))
            {
                if (!PersistentData.SpeciesCounts.ContainsKey(species.Name))
                    return true;
                var effectiveLimit = Math.Round(species.SpeciesLimit * GameSettings.Current.SpeciesLimitAdjust);
                return PersistentData.SpeciesCounts[species.Name] < effectiveLimit;
            }
            else
                return true; // Sure spawn null, go ahead.
        }
    }
}
