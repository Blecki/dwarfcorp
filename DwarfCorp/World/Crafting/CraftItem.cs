using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class CraftItem
    {
        public enum CraftType
        {
            Object,
            Resource
        }

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
            Object
        }

        public string Name = "";
        public string EntityName = "";
        public string ObjectName = "";

        public String DisplayName = null;
        public String ShortDisplayName = null;
        public String PluralDisplayName = null;

        public List<Quantitiy<Resource.ResourceTags>> RequiredResources = new List<Quantitiy<Resource.ResourceTags>>();
        public Gui.TileReference Icon = null;
        public float BaseCraftTime = 0.0f;
        public string Description = "";
        public CraftType Type = CraftType.Object;
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
        public bool Moveable = false;
        public bool Deconstructable = true;
        public CraftActBehaviors CraftActBehavior = CraftActBehaviors.Normal;
        public bool AllowRotation = false;
        public string Category = "";
        public bool IsMagical = false;
        public string Tutorial = "";
        public bool AllowUserCrafting = true;

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

        private IEnumerable<ResourceAmount> MergeResources(IEnumerable<ResourceAmount> resources)
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
                yield return new ResourceAmount(count.Key, count.Value);
            }
        }

        public Resource ToResource(WorldManager world, List<ResourceAmount> selectedResources, string prefix = "")
        {
            var objectName = String.IsNullOrEmpty(ObjectName) ? Name : ObjectName;
            string resourceName = prefix + objectName + " (" + TextGenerator.GetListString(MergeResources(selectedResources).Select(r => (string)r.Type)) + ")";

            if (ResourceLibrary.Exists(resourceName))
                return ResourceLibrary.GetResourceByName(resourceName);

            var sheet = world.UserInterface.Gui.RenderData.SourceSheets[Icon.Sheet];

            var tex = AssetManager.GetContentTexture(sheet.Texture);
            var numTilesX = tex.Width / sheet.TileWidth;
            var numTilesY = tex.Height / sheet.TileHeight;
            var point = new Point(Icon.Tile % numTilesX, Icon.Tile / numTilesX);
            var toReturn = ResourceLibrary.GenerateResource();
            toReturn.Name = resourceName;
            toReturn.Tags = new List<Resource.ResourceTags>()
                    {
                        Resource.ResourceTags.CraftItem,
                        Resource.ResourceTags.Craft
                    };
            toReturn.MoneyValue = selectedResources.Sum(r => ResourceLibrary.GetResourceByName(r.Type).MoneyValue) * 2.0m;
            toReturn.CraftInfo = new Resource.CraftItemInfo
            {
                Resources = selectedResources,
                CraftItemType = objectName
            };
            toReturn.ShortName = Name;
            toReturn.Description = Description;
            toReturn.GuiLayers = new List<Gui.TileReference>() { Icon };
            toReturn.CompositeLayers = new List<Resource.CompositeLayer>() { new Resource.CompositeLayer() { Asset = sheet.Texture, Frame = point, FrameSize = new Point(sheet.TileWidth, sheet.TileHeight) } };
            toReturn.Tint = Color.White;
            ResourceLibrary.Add(toReturn);

            return toReturn;
        }

        public CraftItem ObjectAsCraftableResource()
        {
            string resourceName = Name + "...";
            var toReturn = Library.GetCraftable(resourceName);
            if (toReturn == null)
            {
                toReturn = this.MemberwiseClone() as CraftItem;
                toReturn.Name = resourceName;
                toReturn.Type = CraftType.Resource;
                toReturn.CraftActBehavior = CraftActBehaviors.Object;
                toReturn.ResourceCreated = "Object";
                toReturn.CraftLocation = String.IsNullOrEmpty(CraftLocation) ? "Anvil" : CraftLocation;
                toReturn.ObjectName = Name;
                toReturn.AllowUserCrafting = false;
                Library.AddCraftable(toReturn);
            }
            return toReturn;
        }
    }
}
