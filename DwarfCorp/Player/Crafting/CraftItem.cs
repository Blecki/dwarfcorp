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

        public enum CraftActBehaviors
        {
            Normal,
            Trinket,
            Meal,
            Ale,
            Bread,
            GemTrinket,
        }

        public string Name = "";
        public string EntityName = "";
        public string ObjectName = "";

        public String DisplayName { get; set; }
        public String ShortDisplayName = null;
        public String PluralDisplayName = null;

        public List<ResourceTagAmount> RequiredResources = new List<ResourceTagAmount>();
        public Gui.TileReference Icon { get; set; }
        public float BaseCraftTime = 0.0f;
        public string Description = "";
        public List<CraftPrereq> Prerequisites = new List<CraftPrereq>();
        public int CraftedResultsCount = 1;
        public String ResourceCreated = "";
        public string CraftLocation = "Anvil";
        public string Verb = null;
        public string PastTeseVerb = null;
        public string CurrentVerb = null;
        public bool AllowHeterogenous = false;
        public Vector3 SpawnOffset = new Vector3(0.0f, 0.5f, 0.0f);
        public bool AddToOwnedPool = false;
        public bool Deconstructable = true;
        public CraftActBehaviors CraftActBehavior = CraftActBehaviors.Normal;
        public bool AllowRotation = false;
        public string Category = "";
        public String GetCategory => Category;
        public bool IsMagical = false;
        public string Tutorial = "";
        public bool AllowUserCrafting = true;
        public TaskCategory CraftTaskCategory = TaskCategory.CraftItem;
        public string CraftNoise = "Craft";
        public DwarfBux MoneyValue = 20.0m;

        public bool Disable = false;

        public void InitializeStrings()
        {
            DisplayName = Library.TransformDataString(DisplayName, Name);
            PluralDisplayName = Library.TransformDataString(PluralDisplayName, DisplayName + "s"); // Default to appending an s if the plural name is not specified.
            ShortDisplayName = Library.TransformDataString(ShortDisplayName, DisplayName);
            Verb = Library.TransformDataString(Verb, Library.GetString("build"));
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
