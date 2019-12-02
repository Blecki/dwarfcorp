using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class ResourceType : CraftableRecord
    {
        public struct TrinketInfo
        {
            public string BaseAsset;
            public string EncrustingAsset;
            public int SpriteRow;
            public int SpriteColumn;
        }

        public struct CompositeLayer
        {
            public string Asset;
            public Point FrameSize;
            public Point Frame;
        }

        public string TypeName;
        public string DisplayName { get; set; }
        public Gui.TileReference Icon { get => GuiLayers[0]; }

        public DwarfBux MoneyValue;
        public string Description;
        public List<TileReference> GuiLayers; // Todo: Would like to combine the different graphics options
        public List<String> Tags;
        public float FoodContent;
        public List<CompositeLayer> CompositeLayers;
        public TrinketInfo TrinketData;
        public string PlantToGenerate;
        public Color Tint;
        public string AleName = "";
        public Potion PotionType = null;
        public string Category = "";
        public String GetCategory => Category;

        #region Tool
        public bool Tool_Breakable = true;
        public float Tool_Durability = 1.0f;     // How much wear an item can take.
        public float Tool_Wear = 0.0f;
        public float Tool_Effectiveness = 1.0f;  // How effective is the tool at completing tasks?
        public CharacterMode Tool_AttackAnimation = CharacterMode.Attacking;
        public int Tool_AttackTriggerFrame = 1;
        public float Tool_AttackDamage = 1;
        public String Tool_AttackHitParticles = "";
        public String Tool_AttackHitEffect = "";
        public Color Tool_AttackHitColor = Color.White;
        #endregion

        #region Equipment
        public String Equipment_LayerName = "";
        public String Equipment_LayerType = "Tool";
        public String Equipment_Palette = "Base";
        public EquipmentSlot Equipment_Slot = EquipmentSlot.None;
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

        #endregion

        public ResourceType()
        {

        }
    }
}