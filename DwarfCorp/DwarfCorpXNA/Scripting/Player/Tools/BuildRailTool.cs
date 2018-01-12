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
    public class BuildRailTool : PlayerTool
    {
        public Rail.JunctionPattern Pattern;
        private List<RailPiece> PreviewBodies = new List<RailPiece>();
        private Faction Faction;
        private bool RightPressed = false;
        private bool LeftPressed = false;
        public List<ResourceAmount> SelectedResources;

        private static CraftItem RailCraftItem = new CraftItem
        {
            Description = "Rail.",
            RequiredResources = new List<Quantitiy<Resource.ResourceTags>>
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Metal, 2)
                        },
            Icon = new Gui.TileReference("beartrap", 0),
            BaseCraftTime = 10,
            Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround },
            CraftLocation = "",
            Name = "Rail",
            Type = CraftItem.CraftType.Object,
            AddToOwnedPool = false,
            Moveable = false            
        };

        public BuildRailTool(GameMaster Player)
        {
            this.Player = Player;
            this.Faction = Player.Faction;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
                if (CanPlace(Player.VoxSelector.VoxelUnderMouse))
                {
                    Place(Player.VoxSelector.VoxelUnderMouse);
                    PreviewBodies.Clear();
                    CreatePreviewBodies(Player.World.ComponentManager, Player.VoxSelector.VoxelUnderMouse);
                }
        }

        public override void OnBegin()
        {
            System.Diagnostics.Debug.Assert(Pattern != null);
            System.Diagnostics.Debug.Assert(SelectedResources != null);
            CreatePreviewBodies(Faction.World.ComponentManager, new VoxelHandle(Faction.World.ChunkManager.ChunkData, new GlobalVoxelCoordinate(0, 0, 0)));
        }

        public override void OnEnd()
        {
            foreach (var body in PreviewBodies)
                body.Delete();
            PreviewBodies.Clear();
            Pattern = null;
            SelectedResources = null;
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {

        }

        private void CreatePreviewBodies(ComponentManager ComponentManager, VoxelHandle Location)
        {
            foreach (var body in PreviewBodies)
                body.Delete();

            PreviewBodies.Clear();
            foreach (var piece in Pattern.Pieces)
                PreviewBodies.Add(new RailPiece(ComponentManager, Location, piece));

            foreach (var body in PreviewBodies)
                ComponentManager.RootComponent.AddChild(body);

            foreach (var body in PreviewBodies)
                body.SetFlagRecursive(GameComponent.Flag.Active, false);

            // Todo: Add CraftDetails component.
        }

        private void UpdatePreviewBodies(VoxelHandle Location)
        {
            System.Diagnostics.Debug.Assert(PreviewBodies.Count == Pattern.Pieces.Count);
            for (var i = 0; i < PreviewBodies.Count; ++i)
                PreviewBodies[i].UpdatePiece(Pattern.Pieces[i], Location);
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

            KeyboardState state = Keyboard.GetState();
            bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
            bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
            if (LeftPressed && !leftKey)
                Pattern = Pattern.Rotate(Rail.Orientation.East);
            if (RightPressed && !rightKey)
                Pattern = Pattern.Rotate(Rail.Orientation.West);
            LeftPressed = leftKey;
            RightPressed = rightKey;

            var voxelUnderMouse = Player.VoxSelector.VoxelUnderMouse;
            UpdatePreviewBodies(voxelUnderMouse);

            var tint = Color.White;
            if (CanPlace(voxelUnderMouse))
                tint = Color.Green;
            else
                tint = Color.Red;
            foreach (var body in PreviewBodies)
                body.SetTintRecursive(tint);
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {

        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        private bool CanPlace(VoxelHandle Location, Rail.JunctionPiece Piece, RailPiece PreviewEntity)
        {
            var actualPosition = new VoxelHandle(Location.Chunk.Manager.ChunkData, Location.Coordinate + new GlobalVoxelOffset(Piece.Offset.X, 0, Piece.Offset.Y));
            if (!actualPosition.IsValid) return false;
            if (!actualPosition.IsEmpty) return false;

            if (actualPosition.Coordinate.Y == 0) return false; // ???

            var local = actualPosition.Coordinate.GetLocalVoxelCoordinate();
            var voxelUnder = new VoxelHandle(actualPosition.Chunk, new LocalVoxelCoordinate(local.X, local.Y - 1, local.Z));
            if (voxelUnder.IsEmpty) return false;

            foreach (var entity in  Player.World.CollisionManager.EnumerateIntersectingObjects(actualPosition.GetBoundingBox().Expand(-0.2f), CollisionManager.CollisionType.Static))
            {
                if (Object.ReferenceEquals(entity, PreviewEntity)) continue;

                if (FindPossibleCombination(Piece, entity) != null)
                    return true;

                if (GamePerformance.DebugVisualizationEnabled)
                {
                    Drawer3D.DrawBox(entity.GetBoundingBox(), Color.Yellow, 0.1f, false);
                }

                return false;
            }

            return true;
        }

        private static Rail.RailCombination FindPossibleCombination(Rail.JunctionPiece Piece, IBoundedObject Entity)
        {
            if (Entity is RailPiece)
            {
                var baseJunction = (Entity as RailPiece).Piece;
                var basePiece = Rail.RailLibrary.GetRailPiece(baseJunction.RailPiece);
                var relativeOrientation = Rail.OrientationHelper.Relative(baseJunction.Orientation, Piece.Orientation);
                var matchingCombination = basePiece.CombinationTable.FirstOrDefault(c => c.Overlay == Piece.RailPiece && c.OverlayRelativeOrientation == relativeOrientation);
                return matchingCombination;
            }

            return null;
        }

        private bool CanPlace(VoxelHandle Location)
        {
            for (var i = 0; i < Pattern.Pieces.Count; ++i)
                if (!CanPlace(Location, Pattern.Pieces[i], PreviewBodies[i]))
                    return false;
            return true;
        }

        private void Place(VoxelHandle Location)
        {
            var assignments = new List<Task>();

            for (var i = 0; i < PreviewBodies.Count; ++i)
            {
                var body = PreviewBodies[i];
                var piece = Pattern.Pieces[i];
                var actualPosition = new VoxelHandle(Location.Chunk.Manager.ChunkData, Location.Coordinate + new GlobalVoxelOffset(piece.Offset.X, 0, piece.Offset.Y));
                var addNewDesignation = true;

                foreach (var entity in Player.World.CollisionManager.EnumerateIntersectingObjects(actualPosition.GetBoundingBox().Expand(-0.2f), CollisionManager.CollisionType.Static))
                {
                    if (!addNewDesignation) break;
                    if (Object.ReferenceEquals(entity, body)) continue;

                    var possibleCombination = FindPossibleCombination(piece, entity);
                    if (possibleCombination != null)
                    {
                        var combinedPiece = new Rail.JunctionPiece
                        {
                            RailPiece = possibleCombination.Result,
                            Orientation = Rail.OrientationHelper.Rotate((entity as RailPiece).Piece.Orientation, (int)possibleCombination.ResultRelativeOrientation),
                        };

                        var existingDesignation = Player.Faction.Designations.EnumerateEntityDesignations(DesignationType.Craft).FirstOrDefault(d => Object.ReferenceEquals(d.Body, entity));
                        if (existingDesignation != null)
                        {
                            (entity as RailPiece).UpdatePiece(combinedPiece, actualPosition);
                            (existingDesignation.Tag as CraftDesignation).Progress = 0.0f;
                            body.Delete();
                            addNewDesignation = false;
                        }
                        else
                        {
                            (entity as RailPiece).Die();
                            body.UpdatePiece(combinedPiece, actualPosition);
                        }
                    }
                }

                if (addNewDesignation)
                {

                    var startPos = body.Position + new Vector3(0.0f, -0.3f, 0.0f);
                    var endPos = body.Position;

                    var designation = new CraftDesignation
                    {
                        Entity = body,
                        WorkPile = new WorkPile(Player.World.ComponentManager, startPos),
                        OverrideOrientation = false,
                        Valid = true,
                        ItemType = RailCraftItem,
                        SelectedResources = SelectedResources,
                        Location = new VoxelHandle(Player.World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(body.Position)),
                        HasResources = false,
                        ResourcesReservedFor = null,
                        Orientation = 0.0f,
                        Progress = 0.0f,
                    };

                    Player.World.ComponentManager.RootComponent.AddChild(designation.WorkPile);
                    designation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), endPos));
                    Player.World.ParticleManager.Trigger("puff", endPos, Color.White, 10);
                    Player.Faction.Designations.AddEntityDesignation(body, DesignationType.Craft, designation);
                    assignments.Add(new CraftItemTask(designation));
                }
            }

            if (assignments.Count > 0)
                Player.World.Master.TaskManager.AddTasks(assignments);
        }
    }
}
