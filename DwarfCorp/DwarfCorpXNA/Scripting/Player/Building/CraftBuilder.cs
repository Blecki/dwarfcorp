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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

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
            public CraftItem ItemType { get; set; }
            public VoxelHandle Location { get; set; }
            public Body WorkPile { get; set; }
        }

        public Faction Faction { get; set; }
        public List<CraftDesignation> Designations { get; set; }
        public CraftItem CurrentCraftType { get; set; }
        public bool IsEnabled { get; set; }
        public Body CurrentCraftBody { get; set; }
        protected CraftDesignation CurrentDesignation { get; set; }

        [JsonIgnore]
        private WorldManager World { get; set; }

        [OnDeserialized]
        public void OnDeserializing(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
        }

        public CraftBuilder()
        {
            IsEnabled = false;
        }

        public CraftBuilder(Faction faction, WorldManager world)
        {
            World = world;
            Faction = faction;
            Designations = new List<CraftDesignation>();
            IsEnabled = false;
        }

        public bool IsDesignation(VoxelHandle reference)
        {
            if (reference == null) return false;
            return Designations.Any(put => (put.Location.Position - reference.Position).LengthSquared() < 0.1);
        }


        public CraftDesignation GetDesignation(VoxelHandle v)
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


        public void RemoveDesignation(VoxelHandle v)
        {
            CraftDesignation des = GetDesignation(v);

            if (des != null)
            {
                RemoveDesignation(des);
            }
        }

        private void SetDisplayColor(Color color)
        {
            foreach (var sprite in CurrentCraftBody.EnumerateAll().OfType<Tinter>())
                sprite.VertexColorTint = color;
        }

        public void Update(DwarfTime gameTime, GameMaster player)
        {
            if (!IsEnabled)
            {
                if (CurrentCraftBody != null)
                {
                    CurrentCraftBody.Delete();
                    CurrentCraftBody = null;
                }
                return;
            }

            if (Faction == null)
            {
                Faction = player.Faction;
            }

            if (CurrentCraftType != null && CurrentCraftBody == null)
            {
                CurrentCraftBody = EntityFactory.CreateEntity<Body>(CurrentCraftType.Name, player.VoxSelector.VoxelUnderMouse.Position);
                CurrentCraftBody.SetFlagRecursive(GameComponent.Flag.Active, false);
                CurrentDesignation = new CraftDesignation()
                {
                    ItemType = CurrentCraftType,
                    Location = new VoxelHandle(new LocalVoxelCoordinate(0, 0, 0), null)
                };
                SetDisplayColor(Color.Green);
            }

            if (CurrentCraftBody == null || player.VoxSelector.VoxelUnderMouse == null) 
                return;

            CurrentCraftBody.LocalPosition = player.VoxSelector.VoxelUnderMouse.Position + Vector3.One * 0.5f;
            CurrentCraftBody.GlobalTransform = CurrentCraftBody.LocalTransform;
            CurrentCraftBody.OrientToWalls();

            if (CurrentDesignation.Location.IsSameAs(player.VoxSelector.VoxelUnderMouse)) 
                return;
            
            CurrentDesignation.Location = new VoxelHandle(player.VoxSelector.VoxelUnderMouse);

            SetDisplayColor(IsValid(CurrentDesignation) ? Color.Green : Color.Red);
        }

        public void Render(DwarfTime gameTime, GraphicsDevice graphics, Effect effect)
        {
        }


        public bool IsValid(CraftDesignation designation)
        {
            if (IsDesignation(designation.Location))
            {
                World.ShowToolPopup("Something is already being built there!");
                return false;
            }

            if (!String.IsNullOrEmpty(designation.ItemType.CraftLocation) &&
                Faction.FindNearestItemWithTags(designation.ItemType.CraftLocation, designation.Location.Position, false) ==
                null)
            {
                World.ShowToolPopup("Can't build, need " + designation.ItemType.CraftLocation);
                return false;
            }

            if (!Faction.HasResources(designation.ItemType.RequiredResources))
            {
                string neededResources = "";

                foreach (Quantitiy<Resource.ResourceTags> amount in designation.ItemType.RequiredResources)
                {
                    neededResources += "" + amount.NumResources + " " + amount.ResourceType.ToString() + " ";
                }

                World.ShowToolPopup("Not enough resources! Need " + neededResources + ".");
                return false;
            }

            VoxelHandle[] neighbors = new VoxelHandle[4];
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
                            World.ShowToolPopup("Must be built next to wall!");
                            return false;
                        }

                        break;
                    }
                    case CraftItem.CraftPrereq.OnGround:
                    {
                        VoxelHandle below = new VoxelHandle();
                        designation.Location.GetNeighbor(Vector3.Down, ref below);

                        if (below.IsEmpty)
                        {
                            World.ShowToolPopup("Must be built on solid ground!");
                            return false;
                        }
                        break;
                    }
                }
            }

            return true;

        }

        public void VoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            if (!IsEnabled)
            {
                return;
            }
            switch (button)
            {
                case (InputManager.MouseButton.Left):
                    {
                        if (Faction.FilterMinionsWithCapability(Faction.SelectedMinions, GameMaster.ToolMode.Craft).Count == 0)
                        {
                            World.ShowToolPopup("None of the selected units can craft items.");
                            return;
                        }
                        List<Task> assignments = new List<Task>();
                        foreach (VoxelHandle r in refs)
                        {
                            if (IsDesignation(r) ||r == null || !r.IsEmpty)
                            {
                                continue;
                            }
                            else
                            {
                                Vector3 pos = r.Position + Vector3.One*0.5f;
                                Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);
                                Vector3 endPos = pos;
                                CraftDesignation newDesignation = new CraftDesignation()
                                {
                                    ItemType = CurrentCraftType,
                                    Location = r,
                                    WorkPile = new WorkPile(World.ComponentManager, startPos)
                                };
                                World.ComponentManager.RootComponent.AddChild(newDesignation.WorkPile);
                                newDesignation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), endPos));
                                World.ParticleManager.Trigger("puff", pos, Color.White, 10);
                                if (IsValid(newDesignation))
                                {
                                    AddDesignation(newDesignation);
                                    assignments.Add(new CraftItemTask(new VoxelHandle(r.GridPosition, r.Chunk),
                                        CurrentCraftType));
                                }
                                else
                                {
                                    newDesignation.WorkPile.Die();
                                }
                            }
                        }

                        if (assignments.Count > 0)
                        {
                            TaskManager.AssignTasks(assignments, Faction.FilterMinionsWithCapability(World.Master.SelectedMinions, GameMaster.ToolMode.Craft));
                        }

                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        foreach (VoxelHandle r in refs)
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
