using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A designation specifying that a creature should put a voxel of a given type
    /// at a location.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CraftBuilder
    {
        public class CraftDesignation
        {
            public CraftLibrary.CraftItemType ItemType { get; set; }
            public Voxel Location { get; set; }
        }

        public Faction Faction { get; set; }
        public List<CraftDesignation> Designations { get; set; }
        public CraftLibrary.CraftItemType CurrentCraftType { get; set; }
        public bool IsEnabled { get; set; }

        public CraftBuilder()
        {
            IsEnabled = false;
        }

        public CraftBuilder(Faction faction)
        {
            Faction = faction;
            Designations = new List<CraftDesignation>();
            IsEnabled = false;
        }

        public bool IsDesignation(Voxel reference)
        {
            if (reference == null) return false;
            return Designations.Any(put => (put.Location.Position - reference.Position).LengthSquared() < 0.1);
        }


        public CraftDesignation GetDesignation(Voxel v)
        {
            return Designations.FirstOrDefault(put => (put.Location.Position - v.Position).LengthSquared() < 0.1);
        }

        public void AddDesignation(CraftDesignation des)
        {
            Designations.Add(des);
        }

        public void RemoveDesignation(CraftDesignation des)
        {
            Designations.Remove(des);
        }


        public void RemoveDesignation(Voxel v)
        {
            CraftDesignation des = GetDesignation(v);

            if (des != null)
            {
                RemoveDesignation(des);
            }
        }


        public void Render(GameTime gametime, GraphicsDevice graphics, Effect effect)
        {
            foreach (CraftDesignation designation in Designations)
            {
                Drawer3D.DrawBox(designation.Location.GetBoundingBox(), Color.PaleVioletRed, 0.1f, true);
            }
        }


        public bool IsValid(CraftDesignation designation)
        {
            if (IsDesignation(designation.Location))
            {
                PlayState.GUI.ToolTipManager.Popup("Something is already being built there!");
                return false;
            }

            if (!Faction.HasResources(CraftLibrary.CraftItems[designation.ItemType].RequiredResources))
            {
                string neededResources = "";

                foreach (ResourceAmount amount in CraftLibrary.CraftItems[designation.ItemType].RequiredResources)
                {
                    neededResources += "" + amount.NumResources + " " + amount.ResourceType.ResourceName + " ";
                }

                PlayState.GUI.ToolTipManager.Popup("Not enough resources! Need " + neededResources + ".");
                return false;
            }

            return true;

        }

        public void VoxelsSelected(List<Voxel> refs, InputManager.MouseButton button)
        {
            if (!IsEnabled)
            {
                return;
            }
            switch (button)
            {
                case (InputManager.MouseButton.Left):
                    {
                        List<Task> assignments = new List<Task>();
                        foreach (Voxel r in refs)
                        {
                            if (IsDesignation(r) ||r == null || !r.IsEmpty)
                            {
                                continue;
                            }
                            else
                            {
                                CraftDesignation newDesignation = new CraftDesignation()
                                {
                                    ItemType = CurrentCraftType,
                                    Location = r
                                };

                                if (IsValid(newDesignation))
                                {
                                    AddDesignation(newDesignation);
                                    assignments.Add(new CraftItemTask(new Voxel(new Point3(r.GridPosition), r.Chunk),
                                        CurrentCraftType));
                                }
                            }
                        }

                        if (assignments.Count > 0)
                        {
                            TaskManager.AssignTasks(assignments, PlayState.Master.FilterMinionsWithCapability(PlayState.Master.SelectedMinions, GameMaster.ToolMode.Craft));
                        }

                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        foreach (Voxel r in refs)
                        {
                            if (!IsDesignation(r))
                            {
                                continue;
                            }
                            RemoveDesignation(r);
                        }
                        break;
                    }
            }
        }
    }

}