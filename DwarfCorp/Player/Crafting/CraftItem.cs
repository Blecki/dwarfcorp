using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public interface CraftableRecord // Todo: This is horrible.
    {
        String DisplayName { get; }
        Gui.TileReference Icon { get; } 
        String GetCategory { get; }
    }

    public class CraftItem : CraftableRecord
    {
        public enum CraftPrereq
        {
            OnGround,
            NearWall
        }

        public string Name = "";

        public String DisplayName { get; set; }
        public String PluralDisplayName = null;

        public List<ResourceTagAmount> RequiredResources = new List<ResourceTagAmount>();
        public Gui.TileReference Icon { get; set; }
        public float BaseCraftTime = 0.0f;
        public string Description = "";
        public List<CraftPrereq> Prerequisites = new List<CraftPrereq>();
        public int CraftedResultsCount = 1;
        public String ResourceCreated = "";
        public string CraftLocation = "Anvil";
        public string Verb = null; // Todo: Need 'verb' abstraction to handle tenses
        public string PastTeseVerb = null; // Ugh
        public string CurrentVerb = null;
        public Vector3 SpawnOffset = new Vector3(0.0f, 0.5f, 0.0f); // Only used by god mode tool apparently
        public bool AddToOwnedPool = false;
        public bool Deconstructable = true;
        public String CraftActBehavior = "Normal";
        public string Category = "";
        public String GetCategory => Category;
        public string Tutorial = "";
        public TaskCategory CraftTaskCategory = TaskCategory.CraftItem;
        public string CraftNoise = "Craft";

        public bool Disable = false;

        public void InitializeStrings()
        {
            DisplayName = Library.TransformDataString(DisplayName, Name);
            PluralDisplayName = Library.TransformDataString(PluralDisplayName, DisplayName + "s"); // Default to appending an s if the plural name is not specified.
            Verb = Library.TransformDataString(Verb, Library.GetString("build")); // Todo: Don't like this. Translate by changing the individual data files?
            PastTeseVerb = Library.TransformDataString(PastTeseVerb, Library.GetString("built"));
            CurrentVerb = Library.TransformDataString(CurrentVerb, Library.GetString("building"));
            Description = Library.TransformDataString(Description, Description);
        }

        private IEnumerable<ResourceTypeAmount> MergeResources(IEnumerable<ResourceTypeAmount> resources)
        {
            Dictionary<String, int> counts = new Dictionary<String, int>();
            foreach(var resource in resources)
            {
                if(!counts.ContainsKey(resource.Type))
                {
                    counts.Add(resource.Type, 0);
                }
                counts[resource.Type] += resource.Count;
            }

            foreach(var count in counts)
            {
                yield return new ResourceTypeAmount(count.Key, count.Value);
            }
        }
    }
}
