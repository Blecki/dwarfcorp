// BuildTool.cs
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
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Input;


namespace DwarfCorp
{
    public class BuildObjectTool : PlayerTool
    {
        public Faction Faction { get; set; }
        public CraftItem CurrentCraftType { get; set; }
        public Body CurrentCraftBody { get; set; }
        public List<ResourceAmount> SelectedResources;
        protected CraftDesignation CurrentDesignation;
        private float CurrentOrientation = 0.0f;
        private bool OverrideOrientation = false;
        private bool rightPressed = false;
        private bool leftPressed = false;

        [JsonIgnore]
        public WorldManager World { get; set; }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            switch (button)
            {
                case (InputManager.MouseButton.Left):
                    {
                        List<Task> assignments = new List<Task>();
                        // Creating multiples doesn't work anyway - kill it.
                        foreach (var r in voxels)
                        {
                            if (!r.IsValid || !r.IsEmpty)
                            {
                                continue;
                            }
                            else
                            {
                                Vector3 pos = r.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CurrentCraftType.SpawnOffset;

                                Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);
                                Vector3 endPos = pos;
                                // TODO: Why are we creating a new designation?
                                CraftDesignation newDesignation = new CraftDesignation()
                                {
                                    ItemType = CurrentCraftType,
                                    Location = r,
                                    Orientation = CurrentDesignation.Orientation,
                                    OverrideOrientation = CurrentDesignation.OverrideOrientation,
                                    Valid = true,
                                    Entity = CurrentCraftBody,
                                    SelectedResources = SelectedResources
                                };
                                CurrentCraftBody.SetFlag(GameComponent.Flag.ShouldSerialize, true);

                                if (newDesignation.OverrideOrientation)
                                    newDesignation.Entity.Orient(newDesignation.Orientation);
                                else
                                    newDesignation.Entity.OrientToWalls();

                                if (IsValid(newDesignation))
                                {
                                    var task = new CraftItemTask(newDesignation);

                                    assignments.Add(task);

                                    // Todo: Maybe don't support creating huge numbers of entities at once?
                                    CurrentCraftBody = EntityFactory.CreateEntity<Body>(CurrentCraftType.EntityName, r.WorldPosition,
                                    Blackboard.Create<List<ResourceAmount>>("Resources", SelectedResources));
                                    CurrentCraftBody.SetFlagRecursive(GameComponent.Flag.Active, false);
                                    CurrentCraftBody.SetTintRecursive(Color.White);

                                    newDesignation.WorkPile = new WorkPile(World.ComponentManager, startPos);
                                    World.ComponentManager.RootComponent.AddChild(newDesignation.WorkPile);
                                    newDesignation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), endPos));
                                    World.ParticleManager.Trigger("puff", pos, Color.White, 10);
                                }
                            }
                        }

                        if (assignments.Count > 0)
                        {
                            World.Master.TaskManager.AddTasks(assignments);
                        }

                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        foreach (var r in voxels)
                        {
                            if (r.IsValid)
                            {
                                var designation = Faction.Designations.EnumerateEntityDesignations(DesignationType.Craft).Select(d => d.Tag as CraftDesignation).FirstOrDefault(d => d.Location == r);
                                if (designation != null)
                                {
                                    var realDesignation = World.PlayerFaction.Designations.GetEntityDesignation(designation.Entity, DesignationType.Craft);
                                    if (realDesignation != null)
                                        World.Master.TaskManager.CancelTask(realDesignation.Task);
                                }
                            }
                        }
                        break;
                    }
            }
        }

        public override void OnBegin()
        {
            Player.VoxSelector.DrawBox = false;
            Player.VoxSelector.DrawVoxel = false;
        }

        public override void OnEnd()
        {
            Player.VoxSelector.DrawBox = true;
            Player.VoxSelector.DrawVoxel = true;

            if (CurrentCraftBody != null)
            {
                CurrentCraftBody.Delete();
                CurrentCraftBody = null;
            }

            CurrentCraftType = null;
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                Player.BodySelector.Enabled = false;
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.BodySelector.Enabled = false;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            if (Faction == null)
            {
                Faction = Player.Faction;
            }

            if (CurrentCraftType != null && CurrentCraftBody == null)
            {
                CurrentCraftBody = EntityFactory.CreateEntity<Body>(CurrentCraftType.EntityName,
                    Player.VoxSelector.VoxelUnderMouse.WorldPosition,
                     Blackboard.Create<List<ResourceAmount>>("Resources", SelectedResources));

                CurrentCraftBody.SetFlag(GameComponent.Flag.Active, false);
                CurrentCraftBody.SetTintRecursive(Color.White);
                CurrentCraftBody.SetFlagRecursive(GameComponent.Flag.ShouldSerialize, false);

                CurrentDesignation = new CraftDesignation()
                {
                    ItemType = CurrentCraftType,
                    Location = VoxelHandle.InvalidHandle,
                    Valid = true
                };

                OverrideOrientation = false;
                CurrentCraftBody.SetTintRecursive(Color.Green);
            }

            if (CurrentCraftBody == null || !Player.VoxSelector.VoxelUnderMouse.IsValid)
                return;

            CurrentCraftBody.LocalPosition = Player.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CurrentCraftType.SpawnOffset;

            CurrentCraftBody.UpdateTransform();
            CurrentCraftBody.PropogateTransforms();
            var tinters = CurrentCraftBody.EnumerateAll().OfType<Tinter>();
            foreach (var tinter in tinters)
            {
                tinter.Stipple = true;
            }
            if (OverrideOrientation)
            {
                CurrentCraftBody.Orient(CurrentOrientation);
            }
            else
            {
                CurrentCraftBody.OrientToWalls();
            }

            HandleOrientation();

            if (CurrentDesignation != null)
            {
                if (CurrentDesignation.Location.Equals(Player.VoxSelector.VoxelUnderMouse))
                    return;

                CurrentDesignation.Location = Player.VoxSelector.VoxelUnderMouse;

                World.ShowTooltip("Click to build. Press R/T to rotate.");
                CurrentCraftBody.SetTintRecursive(IsValid(CurrentDesignation) ? Color.Green : Color.Red);
            }
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            if (CurrentCraftBody != null)
            {
                Drawer2D.DrawPolygon(World.Camera, new List<Vector3>() { CurrentCraftBody.Position, CurrentCraftBody.Position + CurrentCraftBody.GlobalTransform.Right * 0.5f }, Color.White, 1, false, graphics.Viewport);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public bool IsValid(CraftDesignation designation)
        {
            if (!designation.Valid)
            {
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
                // Just check for any intersecting body in octtree.

                if (OverrideOrientation)
                {
                    CurrentCraftBody.Orient(CurrentOrientation);
                }
                else
                {
                    CurrentCraftBody.OrientToWalls();
                }

                var intersectsAnyOther = Faction.OwnedObjects.FirstOrDefault(
                    o => o != null &&
                    o != CurrentCraftBody &&
                    o.GetRotatedBoundingBox().Intersects(CurrentCraftBody.GetRotatedBoundingBox().Expand(-0.1f)));

                var intersectsBuildObjects = Faction.Designations.EnumerateEntityDesignations(DesignationType.Craft)
                    .Any(d => d.Body != CurrentCraftBody && d.Body.GetRotatedBoundingBox().Intersects(
                        CurrentCraftBody.GetRotatedBoundingBox().Expand(-0.1f)));

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

        private void HandleOrientation()
        {
            if (CurrentDesignation == null || CurrentCraftBody == null)
            {
                return;
            }

            // Don't attempt any control if the user is trying to type intoa focus item.
            if (World.Gui.FocusItem != null && !World.Gui.FocusItem.IsAnyParentTransparent() && !World.Gui.FocusItem.IsAnyParentHidden())
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
                CurrentOrientation += (float)(Math.PI / 2);
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

    }
}
