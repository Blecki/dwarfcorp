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
using System.Security.Cryptography;
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
            public CraftItem ItemType;
            public VoxelHandle Location;
            public Body WorkPile;
            public bool OverrideOrientation;
            public float Orientation;
            public bool Valid;
            public Body GhostBody;
            public float Progress = 0.0f;
            public bool HasResources = false;
            public CreatureAI ResourcesReservedFor = null;
        }

        public Faction Faction { get; set; }
        public List<CraftDesignation> Designations { get; set; }
        public CraftItem CurrentCraftType { get; set; }
        public bool IsEnabled { get; set; }
        public Body CurrentCraftBody { get; set; }

        public void End()
        {
            if (CurrentCraftBody != null)
            {
                CurrentCraftBody.Delete();
                CurrentCraftBody = null;
            }

            CurrentCraftType = null;
            IsEnabled = false;
        }

        protected CraftDesignation CurrentDesignation;
        private float CurrentOrientation = 0.0f;
        private bool OverrideOrientation = false;
        private bool rightPressed = false;
        private bool leftPressed = false;

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
            if (!reference.IsValid) return false;
            return Designations.Any(put => put.Location == reference);
        }


        public CraftDesignation GetDesignation(VoxelHandle v)
        {
            return Designations.FirstOrDefault(put => put.Location == v);
        }

        public void AddDesignation(CraftDesignation des)
        {
            des.GhostBody = EntityFactory.CreateGhostedEntity<Body>(des.ItemType.Name, 
                des.Location.WorldPosition + Vector3.One * 0.5f, Color.LightBlue, 
                Blackboard.Create<List<ResourceAmount>>("Resources", des.ItemType.SelectedResources));
            World.ComponentManager.RootComponent.AddChild(des.GhostBody);
            if (des.OverrideOrientation)
            {
                des.GhostBody.Orient(des.Orientation);
            }
            else
            {
                des.GhostBody.OrientToWalls();
            }
            Designations.Add(des);
        }

        public void RemoveDesignation(CraftDesignation des)
        {
            Designations.Remove(des);

            if (des.WorkPile != null)
                des.WorkPile.Die();

            if (des.GhostBody != null)
                des.GhostBody.Delete();
        }


        public void RemoveDesignation(VoxelHandle v)
        {
            CraftDesignation des = GetDesignation(v);

            if (des.Valid)
            {
                RemoveDesignation(des);
            }
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

                foreach (var designation in Designations)
                {
                    designation.GhostBody.SetFlagRecursive(GameComponent.Flag.Visible, false);
                }
                return;
            }

            if (Faction == null)
            {
                Faction = player.Faction;
            }

            if (CurrentCraftType != null && CurrentCraftBody == null)
            {
                CurrentCraftBody = EntityFactory.CreateGhostedEntity<Body>(CurrentCraftType.Name, 
                    player.VoxSelector.VoxelUnderMouse.WorldPosition, Color.White,
                     Blackboard.Create<List<ResourceAmount>>("Resources", CurrentCraftType.SelectedResources));
                CurrentDesignation = new CraftDesignation()
                {
                    ItemType = CurrentCraftType,
                    Location = VoxelHandle.InvalidHandle,
                    Valid = true
                };
                OverrideOrientation = false;
                CurrentCraftBody.SetTintRecursive(Color.Green);
            }

            if (CurrentCraftBody == null || !player.VoxSelector.VoxelUnderMouse.IsValid) 
                return;

            CurrentCraftBody.LocalPosition = player.VoxSelector.VoxelUnderMouse.WorldPosition + Vector3.One * 0.5f;

            //Ugly hack
            if (this.CurrentCraftType.Name == "Bed")
                CurrentCraftBody.LocalPosition -= new Vector3(0.0f, 0.5f, 0.0f);

            CurrentCraftBody.GlobalTransform = CurrentCraftBody.LocalTransform;
            CurrentCraftBody.UpdateTransform();
            CurrentCraftBody.PropogateTransforms();
            if (OverrideOrientation)
            {
                CurrentCraftBody.Orient(CurrentOrientation);
            }
            else
            {
                CurrentCraftBody.OrientToWalls();
            }

            HandleOrientation();

            //Todo: Operator == implemented correctly for voxel handles?
            if (CurrentDesignation.Location.Equals(player.VoxSelector.VoxelUnderMouse)) 
                return;

            CurrentDesignation.Location = player.VoxSelector.VoxelUnderMouse;

            World.ShowTooltip("Click to build. Press R/T to rotate.");
            CurrentCraftBody.SetTintRecursive(IsValid(CurrentDesignation) ? Color.Green : Color.Red);
        }

        private void HandleOrientation()
        {
            if (CurrentDesignation == null || CurrentCraftBody == null)
            {
                return;
            }

            KeyboardState state = Keyboard.GetState();
            bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
            bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
            if (leftPressed && !leftKey)
            {
                OverrideOrientation = true;
                leftPressed = false;
                CurrentOrientation += (float) (Math.PI/2);
                CurrentCraftBody.Orient(CurrentOrientation);
                CurrentCraftBody.UpdateBoundingBox();
                CurrentCraftBody.UpdateTransform();
                CurrentCraftBody.PropogateTransforms();
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, CurrentCraftBody.Position,
                    0.5f);
                CurrentCraftBody.SetTintRecursive(IsValid(CurrentDesignation) ? Color.Green : Color.Red);
            }
            if (rightPressed && !rightKey)
            {
                OverrideOrientation = true;
                rightPressed = false;
                CurrentOrientation -= (float)(Math.PI / 2);
                CurrentCraftBody.Orient(CurrentOrientation);
                CurrentCraftBody.UpdateBoundingBox();
                CurrentCraftBody.UpdateTransform();
                CurrentCraftBody.PropogateTransforms();
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, CurrentCraftBody.Position, 0.5f);
                CurrentCraftBody.SetTintRecursive(IsValid(CurrentDesignation) ? Color.Green : Color.Red);
            }


            leftPressed = leftKey;
            rightPressed = rightKey;

            CurrentDesignation.OverrideOrientation = this.OverrideOrientation;
            CurrentDesignation.Orientation = this.CurrentOrientation;
        }


        public void Render(DwarfTime gameTime, GraphicsDevice graphics, Effect effect)
        {
            foreach (var designation in Designations)
            {
                designation.GhostBody.SetFlagRecursive(GameComponent.Flag.Visible, true);
                designation.GhostBody.SetTintRecursive(MathFunctions.Pulsate(Color.Blue, gameTime, 0.7f));
                designation.GhostBody.PropogateTransforms();
            }

            if (CurrentCraftBody != null)
            {
                Drawer2D.DrawPolygon(World.Camera, new List<Vector3>() { CurrentCraftBody.Position, CurrentCraftBody.Position + CurrentCraftBody.GlobalTransform.Right * 0.5f }, Color.White, 1, false, graphics.Viewport);
            }
        }


        public bool IsValid(CraftDesignation designation)
        {
            if (!designation.Valid)
            {
                return false;
            }

            if (IsDesignation(designation.Location))
            {
                World.ShowToolPopup("Something is already being built there!");
                return false;
            }

            if (!String.IsNullOrEmpty(designation.ItemType.CraftLocation) &&
                Faction.FindNearestItemWithTags(designation.ItemType.CraftLocation, designation.Location.WorldPosition, false) ==
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

            foreach (var req in designation.ItemType.Prerequisites)
            {
                switch (req)
                {
                    case CraftItem.CraftPrereq.NearWall:
                        {
                            var neighborFound = VoxelHelpers.EnumerateManhattanNeighbors2D(designation.Location.Coordinate)
                                    .Select(c => new VoxelHandle(World.ChunkManager.ChunkData, c))
                                    .Any(v => v.IsValid && !v.IsEmpty);

                            if (!neighborFound)
                            {
                                World.ShowToolPopup("Must be built next to wall!");
                                return false;
                            }

                            break;
                        }
                    case CraftItem.CraftPrereq.OnGround:
                    {
                            var below = VoxelHelpers.GetNeighbor(designation.Location, new GlobalVoxelOffset(0, -1, 0));

                        if (!below.IsValid || below.IsEmpty)
                        {
                            World.ShowToolPopup("Must be built on solid ground!");
                            return false;
                        }
                        break;
                    }
                }
            }

            if (CurrentCraftBody != null)
            {
                var intersectsAnyOther = Faction.OwnedObjects.FirstOrDefault(
                    o => o != null &&
                    o != CurrentCraftBody &&
                    o.GetRotatedBoundingBox().Intersects(CurrentCraftBody.GetRotatedBoundingBox().Expand(-0.1f)));
                var intersectsBuildObjects = this.Designations.Any(d => d.GhostBody != CurrentCraftBody &&
                    d.GhostBody.GetRotatedBoundingBox().Intersects(CurrentCraftBody.GetRotatedBoundingBox().Expand(-0.1f)));
                bool intersectsWall = VoxelHelpers.EnumerateCoordinatesInBoundingBox
                    (CurrentCraftBody.GetRotatedBoundingBox().Expand(-0.1f)).Any(
                    v =>
                    {
                        var tvh = new VoxelHandle(World.ChunkManager.ChunkData, v);
                        return tvh.IsValid && !tvh.IsEmpty;
                    });

                if (intersectsAnyOther != null)
                {
                    World.ShowToolPopup("Can't build here: intersects " + intersectsAnyOther.Name);
                }
                else if (intersectsBuildObjects)
                {
                    World.ShowToolPopup("Can't build here: intersects something else being built");
                }
                else if (intersectsWall && !designation.ItemType.Prerequisites.Contains(CraftItem.CraftPrereq.NearWall))
                {
                    World.ShowToolPopup("Can't build here: intersects wall.");
                }

                return (intersectsAnyOther == null && !intersectsBuildObjects &&
                       (!intersectsWall || designation.ItemType.Prerequisites.Contains(CraftItem.CraftPrereq.NearWall)));
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
                        List<Task> assignments = new List<Task>();
                        foreach (var r in refs)
                        {
                            if (IsDesignation(r) || !r.IsValid || !r.IsEmpty)
                            {
                                continue;
                            }
                            else
                            {
                                Vector3 pos = r.WorldPosition + Vector3.One*0.5f;

                                // More hack cause I broke beds.
                                if (CurrentCraftType.Name == "Bed") pos -= new Vector3(0, 0.5f, 0);

                                Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);
                                Vector3 endPos = pos;
                                CraftDesignation newDesignation = new CraftDesignation()
                                {
                                    ItemType = CurrentCraftType.Clone(),
                                    Location = r,
                                    WorkPile = new WorkPile(World.ComponentManager, startPos),
                                    Orientation = CurrentDesignation.Orientation,
                                    OverrideOrientation = CurrentDesignation.OverrideOrientation,
                                    Valid = true
                                };
                                World.ComponentManager.RootComponent.AddChild(newDesignation.WorkPile);
                                newDesignation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), endPos));
                                World.ParticleManager.Trigger("puff", pos, Color.White, 10);
                                if (IsValid(newDesignation))
                                {
                                    AddDesignation(newDesignation);
                                    assignments.Add(new CraftItemTask(newDesignation));
                                }
                                else
                                {
                                    newDesignation.WorkPile.Die();
                                }
                            }
                        }

                        if (assignments.Count > 0)
                        {
                            World.Master.TaskManager.AddTasks(assignments);
                            //TaskManager.AssignTasks(assignments, Faction.FilterMinionsWithCapability(World.Master.SelectedMinions, GameMaster.ToolMode.Craft));
                        }

                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        foreach (var r in refs)
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
