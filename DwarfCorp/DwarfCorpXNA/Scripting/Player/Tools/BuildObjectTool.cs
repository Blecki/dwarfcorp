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
        public CraftItem CraftType { get; set; }
        public Body PreviewBody { get; set; }
        public List<ResourceAmount> SelectedResources;
        private float Orientation = 0.0f;
        private bool OverrideOrientation = false;
        private bool RightPressed = false;
        private bool LeftPressed = false;

        [JsonIgnore]
        public WorldManager World { get; set; }

        private Body CreatePreviewBody()
        {
            var previewBody = EntityFactory.CreateEntity<Body>(
                CraftType.EntityName, 
                Player.VoxSelector.VoxelUnderMouse.WorldPosition,
                Blackboard.Create<List<ResourceAmount>>("Resources", SelectedResources));
            previewBody.SetFlagRecursive(GameComponent.Flag.Active, false);
            previewBody.SetTintRecursive(Color.White);
            previewBody.SetFlagRecursive(GameComponent.Flag.ShouldSerialize, false);
            return previewBody;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            switch (button)
            {
                case (InputManager.MouseButton.Left):
                    {
                        if (IsValid(Player.VoxSelector.VoxelUnderMouse))
                        {
                            PreviewBody.SetFlag(GameComponent.Flag.ShouldSerialize, true);

                            Vector3 pos = Player.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CraftType.SpawnOffset;
                            Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);
                         
                           CraftDesignation newDesignation = new CraftDesignation()
                            {
                                ItemType = CraftType,
                                Location = Player.VoxSelector.VoxelUnderMouse,
                                Orientation = Orientation,
                                OverrideOrientation = OverrideOrientation,
                                Valid = true,
                                Entity = PreviewBody,
                                SelectedResources = SelectedResources,
                                WorkPile = new WorkPile(World.ComponentManager, startPos)
                            };

                            World.ComponentManager.RootComponent.AddChild(newDesignation.WorkPile);
                            newDesignation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), pos));
                            World.ParticleManager.Trigger("puff", pos, Color.White, 10);

                            World.Master.TaskManager.AddTask(new CraftItemTask(newDesignation));

                            PreviewBody = CreatePreviewBody();
                        }

                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        var designation = Player.Faction.Designations.EnumerateEntityDesignations(DesignationType.Craft).Select(d => d.Tag as CraftDesignation).FirstOrDefault(d => d.Location == Player.VoxSelector.VoxelUnderMouse);
                        if (designation != null)
                        {
                            var realDesignation = World.PlayerFaction.Designations.GetEntityDesignation(designation.Entity, DesignationType.Craft);
                            if (realDesignation != null)
                                World.Master.TaskManager.CancelTask(realDesignation.Task);
                        }
                        break;
                    }
            }
        }

        public override void OnBegin()
        {
            Player.VoxSelector.DrawBox = false;
            Player.VoxSelector.DrawVoxel = false;

            if (CraftType == null)
                throw new InvalidOperationException();

            PreviewBody = CreatePreviewBody();
            Orientation = 0.0f;
            OverrideOrientation = false;
        }

        public override void OnEnd()
        {
            Player.VoxSelector.DrawBox = true;
            Player.VoxSelector.DrawVoxel = true;

            if (PreviewBody != null)
            {
                PreviewBody.Delete();
                PreviewBody = null;
            }

            CraftType = null;
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

            if (PreviewBody == null || !Player.VoxSelector.VoxelUnderMouse.IsValid)
                return;

            HandleOrientation();

            PreviewBody.LocalPosition = Player.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CraftType.SpawnOffset;
            PreviewBody.UpdateTransform();
            PreviewBody.PropogateTransforms();

            foreach (var tinter in PreviewBody.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            if (OverrideOrientation)
                PreviewBody.Orient(Orientation);
            else
                PreviewBody.OrientToWalls();

            PreviewBody.SetTintRecursive(IsValid(Player.VoxSelector.VoxelUnderMouse) ? Color.Green : Color.Red);

            if (CraftType.AllowRotation)
                World.ShowTooltip("Click to build. Press R/T to rotate.");
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            if (PreviewBody != null)
            {
                Drawer2D.DrawPolygon(World.Camera, new List<Vector3>() { PreviewBody.Position, PreviewBody.Position + PreviewBody.GlobalTransform.Right * 0.5f }, Color.White, 1, false, graphics.Viewport);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public bool IsValid(VoxelHandle Location)
        {            
            if (!String.IsNullOrEmpty(CraftType.CraftLocation) 
                && Player.Faction.FindNearestItemWithTags(CraftType.CraftLocation, Location.WorldPosition, false, null) == null)
            {
                World.ShowToolPopup("Can't build, need " + CraftType.CraftLocation);
                return false;
            }

            foreach (var req in CraftType.Prerequisites)
            {
                switch (req)
                {
                    case CraftItem.CraftPrereq.NearWall:
                        {
                            var neighborFound = VoxelHelpers.EnumerateManhattanNeighbors2D(Location.Coordinate)
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
                            var below = VoxelHelpers.GetNeighbor(Location, new GlobalVoxelOffset(0, -1, 0));

                            if (!below.IsValid || below.IsEmpty)
                            {
                                World.ShowToolPopup("Must be built on solid ground!");
                                return false;
                            }
                            break;
                        }
                }
            }

            if (PreviewBody != null)
            {
                // Just check for any intersecting body in octtree.

                var intersectsAnyOther = Player.Faction.OwnedObjects.FirstOrDefault(o => 
                    o != null 
                    && o != PreviewBody 
                    && o.GetRotatedBoundingBox().Intersects(PreviewBody.GetRotatedBoundingBox().Expand(-0.1f)));

                var intersectsBuildObjects = Player.Faction.Designations.EnumerateEntityDesignations(DesignationType.Craft)
                    .Any(d => d.Body != PreviewBody && d.Body.GetRotatedBoundingBox().Intersects(
                        PreviewBody.GetRotatedBoundingBox().Expand(-0.1f)));

                bool intersectsWall = VoxelHelpers.EnumerateCoordinatesInBoundingBox
                    (PreviewBody.GetRotatedBoundingBox().Expand(-0.1f)).Any(
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
                else if (intersectsWall && !CraftType.Prerequisites.Contains(CraftItem.CraftPrereq.NearWall))
                {
                    World.ShowToolPopup("Can't build here: intersects wall.");
                }

                return (intersectsAnyOther == null && !intersectsBuildObjects &&
                       (!intersectsWall || CraftType.Prerequisites.Contains(CraftItem.CraftPrereq.NearWall)));
            }
            return true;
        }

        private void HandleOrientation()
        {
            // Don't attempt any control if the user is trying to type intoa focus item.
            if (World.Gui.FocusItem == null || World.Gui.FocusItem.IsAnyParentTransparent() || World.Gui.FocusItem.IsAnyParentHidden())
            {
                KeyboardState state = Keyboard.GetState();
                bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
                bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
                if (LeftPressed && !leftKey)
                {
                    OverrideOrientation = true;
                    LeftPressed = false;
                    Orientation += (float)(Math.PI / 2);
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, PreviewBody.Position,
                        0.5f);
                }

                if (RightPressed && !rightKey)
                {
                    OverrideOrientation = true;
                    RightPressed = false;
                    Orientation -= (float)(Math.PI / 2);
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, PreviewBody.Position, 0.5f);
                }


                LeftPressed = leftKey;
                RightPressed = rightKey;
            }
        }
    }
}
