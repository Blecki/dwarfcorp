// CraftBuilder.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A designation specifying that a creature should put a voxel of a given type
    ///     at a location.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CraftBuilder
    {
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

        public Faction Faction { get; set; }
        public List<CraftDesignation> Designations { get; set; }
        public CraftItem CurrentCraftType { get; set; }
        public bool IsEnabled { get; set; }

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

            if (des.WorkPile != null)
                des.WorkPile.Die();
        }


        public void RemoveDesignation(Voxel v)
        {
            CraftDesignation des = GetDesignation(v);

            if (des != null)
            {
                RemoveDesignation(des);
            }
        }


        public void Render(DwarfTime gameTime, GraphicsDevice graphics, Effect effect)
        {
        }


        public bool IsValid(CraftDesignation designation)
        {
            if (IsDesignation(designation.Location))
            {
                PlayState.GUI.ToolTipManager.Popup(Drawer2D.WrapColor("Something is already being built there!",
                    Color.Red));
                return false;
            }

            if (Faction.GetNearestRoomOfType(WorkshopRoom.WorkshopName, designation.Location.Position) == null)
            {
                PlayState.GUI.ToolTipManager.Popup(Drawer2D.WrapColor("Can't build, no workshops!", Color.Red));
                return false;
            }

            if (!Faction.HasResources(designation.ItemType.RequiredResources))
            {
                string neededResources = "";

                foreach (var amount in designation.ItemType.RequiredResources)
                {
                    neededResources += "" + amount.NumResources + " " + amount.ResourceType + " ";
                }

                PlayState.GUI.ToolTipManager.Popup(
                    Drawer2D.WrapColor("Not enough resources! Need " + neededResources + ".", Color.Red));
                return false;
            }

            var neighbors = new Voxel[4];
            foreach (CraftItem.CraftPrereq req in designation.ItemType.Prerequisites)
            {
                switch (req)
                {
                    case CraftItem.CraftPrereq.NearWall:
                    {
                        designation.Location.Chunk.Get2DManhattanNeighbors(neighbors,
                            (int) designation.Location.GridPosition.X,
                            (int) designation.Location.GridPosition.Y, (int) designation.Location.GridPosition.Z);

                        bool neighborFound = neighbors.Any(voxel => voxel != null && !voxel.IsEmpty);

                        if (!neighborFound)
                        {
                            PlayState.GUI.ToolTipManager.Popup(Drawer2D.WrapColor("Must be built next to wall!",
                                Color.Red));
                            return false;
                        }

                        break;
                    }
                    case CraftItem.CraftPrereq.OnGround:
                    {
                        var below = new Voxel();
                        designation.Location.GetNeighbor(Vector3.Down, ref below);

                        if (below.IsEmpty)
                        {
                            PlayState.GUI.ToolTipManager.Popup(Drawer2D.WrapColor("Must be built on solid ground!",
                                Color.Red));
                            return false;
                        }
                        break;
                    }
                }
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
                    if (Faction.FilterMinionsWithCapability(Faction.SelectedMinions, GameMaster.ToolMode.Craft).Count ==
                        0)
                    {
                        PlayState.GUI.ToolTipManager.Popup("None of the selected units can craft items.");
                        return;
                    }
                    var assignments = new List<Task>();
                    foreach (Voxel r in refs)
                    {
                        if (IsDesignation(r) || r == null || !r.IsEmpty)
                        {
                        }
                        Vector3 pos = r.Position + Vector3.One*0.5f;
                        Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);
                        Vector3 endPos = pos;
                        var newDesignation = new CraftDesignation
                        {
                            ItemType = CurrentCraftType,
                            Location = r,
                            WorkPile = new WorkPile(startPos)
                        };

                        newDesignation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f,
                            Matrix.CreateTranslation(startPos), endPos));
                        PlayState.ParticleManager.Trigger("puff", pos, Color.White, 10);
                        if (IsValid(newDesignation))
                        {
                            AddDesignation(newDesignation);
                            assignments.Add(new CraftItemTask(new Voxel(new Point3(r.GridPosition), r.Chunk),
                                CurrentCraftType));
                        }
                        else
                        {
                            newDesignation.WorkPile.Die();
                        }
                    }

                    if (assignments.Count > 0)
                    {
                        TaskManager.AssignTasks(assignments,
                            Faction.FilterMinionsWithCapability(PlayState.Master.SelectedMinions,
                                GameMaster.ToolMode.Craft));
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

        public class CraftDesignation
        {
            public CraftItem ItemType { get; set; }
            public Voxel Location { get; set; }
            public Body WorkPile { get; set; }
        }
    }
}