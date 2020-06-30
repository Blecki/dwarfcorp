using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class ResourceType
    {
        public class TrinketInfo
        {
            public String Name;
            public float Value;
            public GuiGraphic Graphic;
            public GuiGraphic EncrustingGraphic;
        }

        public class GuiGraphic
        {
            public string AssetPath = null;
            public string Palette = "None";
            public Point FrameSize;
            public Point Frame;
            public GuiGraphic NextLayer = null;

            public String GetSheetIdentifier()
            {
                var r = AssetPath + String.Format("/{0}-{1}/{2}-{3}/{4}", FrameSize.X, FrameSize.Y, Frame.X, Frame.Y, Palette);
                if (NextLayer != null)
                    r += "/" + NextLayer.GetSheetIdentifier();
                return r;
            }

            public GuiGraphic Clone()
            {
                var r = new GuiGraphic();
                r.AssetPath = AssetPath;
                r.Palette = Palette;
                r.FrameSize = FrameSize;
                r.Frame = Frame;
                if (NextLayer != null)
                    r.NextLayer = NextLayer.Clone();
                return r;
            }
        }

        public List<TrinketInfo> Trinket_TrinketData = null;
        public TrinketInfo Trinket_EncrustingData = null;
        public string Trinket_JewellPalette = "None";

        public bool Disable = false;

        public string TypeName;
        public string DisplayName { get; set; }
        public String PluralDisplayName = null;

        public DwarfBux MoneyValue;
        public string Description;
        public List<String> Tags;
        public float FoodContent;
        public string PlantToGenerate;
        public string AleName = "";
        public Potion PotionType = null;
        public string Category = "";
        public String Tutorial = "";

        #region Gui
        public GuiGraphic Gui_Graphic = null;
        #endregion

        #region Tool
        public bool Tool_Breakable = true;
        public float Tool_Durability = 1.0f;     // How much wear an item can take.
        public float Tool_Wear = 0.0f;
        public float Tool_Effectiveness = 1.0f;  // How effective is the tool at completing tasks?
        public CharacterMode Tool_AttackAnimation = CharacterMode.Attacking;    // Todo: This should become like... 'UseAnimation'
        public int Tool_AttackTriggerFrame = 1;
        public String Tool_AttackHitParticles = "";
        public String Tool_AttackHitEffect = "";
        public Color Tool_AttackHitColor = Color.White;
        #endregion

        #region Equipment
        public bool Equipable = false;
        public String Equipment_LayerName = "";
        public String Equipment_LayerType = "Tool";
        public String Equipment_Palette = "Base";
        public String Equipment_Slot = "";
        public Weapon Equipment_Weapon = null;
        #endregion

        #region Placement
        public bool Placement_Placeable = false;
        public String Placement_EntityToCreate = null;
        public bool Placement_AllowRotation = false;
        public float Placement_PlaceTime = 10.0f;
        public Vector3 Placement_SpawnOffset = Vector3.Zero;
        public bool Placement_AddToOwnedPool = true;
        public bool Placement_MarkDestructable = true;

        public enum PlacementRequirement
        {
            OnGround,
            NearWall
        }

        public PlacementRequirement Placement_PlacementRequirement = PlacementRequirement.OnGround;
        #endregion

        #region Crafting
        public bool Craft_Craftable = false;
        public List<ResourceTagAmount> Craft_Ingredients = new List<ResourceTagAmount>();
        public float Craft_BaseCraftTime = 0.0f;
        public int Craft_ResultsCount = 1;
        public String Craft_Location = "Anvil";
        public Verb Craft_Verb = new Verb { Base = "Craft", PastTense = "Crafted", PresentTense = "Crafting" };
        public TaskCategory Craft_TaskCategory = TaskCategory.CraftItem;
        public String Craft_Noise = "Craft";
        public String Craft_MetaResourceFactory = "Normal";
        #endregion

        public ResourceType()
        {

        }

        public void InitializeStrings()
        {
            DisplayName = Library.TransformDataString(DisplayName, TypeName);
            PluralDisplayName = Library.TransformDataString(PluralDisplayName, DisplayName + "s"); // Default to appending an s if the plural name is not specified.
            Description = Library.TransformDataString(Description, Description);
        }

    }
}