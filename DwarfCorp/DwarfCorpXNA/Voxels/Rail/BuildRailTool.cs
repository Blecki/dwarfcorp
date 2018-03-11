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

namespace DwarfCorp.Rail
{
    public class BuildRailTool : PlayerTool
    {
        public Rail.JunctionPattern Pattern;
        private List<RailEntity> PreviewBodies = new List<RailEntity>();
        private Faction Faction;
        private bool RightPressed = false;
        private bool LeftPressed = false;
        public List<ResourceAmount> SelectedResources;
        public bool GodModeSwitch = false;

        private static CraftItem RailCraftItem = new CraftItem
        {
            Description = "Rail.",
            RequiredResources = new List<Quantitiy<Resource.ResourceTags>>
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Rail, 1)
                        },
            Icon = new Gui.TileReference("resources", 38),
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
            GodModeSwitch = false;
            CreatePreviewBodies(Faction.World.ComponentManager, new VoxelHandle(Faction.World.ChunkManager.ChunkData, new GlobalVoxelCoordinate(0, 0, 0)));
        }

        public override void OnEnd()
        {
            foreach (var body in PreviewBodies)
                body.Delete();
            PreviewBodies.Clear();
            Pattern = null;
            SelectedResources = null;
            Player.VoxSelector.DrawVoxel = true;
            Player.VoxSelector.DrawBox = true;
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
                PreviewBodies.Add(CreatePreviewBody(ComponentManager, Location, piece));

            // Todo: Add CraftDetails component.
        }

        private RailEntity CreatePreviewBody(ComponentManager Manager, VoxelHandle Location, JunctionPiece Piece)
        {
            var r = new RailEntity(Manager, Location, Piece);
            Manager.RootComponent.AddChild(r);
            r.SetFlagRecursive(GameComponent.Flag.Active, false);

            foreach (var tinter in r.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            r.SetFlag(GameComponent.Flag.ShouldSerialize, false);
            //Todo: Add craft details component.
            return r;
        }

        private void UpdatePreviewBodies(VoxelHandle Location)
        {
            System.Diagnostics.Debug.Assert(PreviewBodies.Count == Pattern.Pieces.Count);
            for (var i = 0; i < PreviewBodies.Count && i < Pattern.Pieces.Count; ++i)
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
            Player.VoxSelector.DrawBox = false;
            Player.VoxSelector.DrawVoxel = false;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            KeyboardState state = Keyboard.GetState();
            bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
            bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
            if (LeftPressed && !leftKey)
                Pattern = Pattern.Rotate(Rail.PieceOrientation.East);
            if (RightPressed && !rightKey)
                Pattern = Pattern.Rotate(Rail.PieceOrientation.West);
            LeftPressed = leftKey;
            RightPressed = rightKey;

            var tint = Color.White;

                var voxelUnderMouse = Player.VoxSelector.VoxelUnderMouse;
                UpdatePreviewBodies(voxelUnderMouse);

                if (CanPlace(voxelUnderMouse))
                    tint = Color.Green;
                else
                    tint = Color.Red;
        
            foreach (var body in PreviewBodies)
                body.SetTintRecursive(tint);
        }

        private GlobalVoxelCoordinate OffsetCoordinateThroughPortal(GlobalVoxelCoordinate C, JunctionPortal Portal)
        {
            switch (Portal.Direction)
            {
                case PieceOrientation.North:
                    return new GlobalVoxelCoordinate(C.X, C.Y, C.Z + 1);
                case PieceOrientation.East:
                    return new GlobalVoxelCoordinate(C.X + 1, C.Y, C.Z);
                case PieceOrientation.South:
                    return new GlobalVoxelCoordinate(C.X, C.Y, C.Z - 1);
                case PieceOrientation.West:
                    return new GlobalVoxelCoordinate(C.X - 1, C.Y, C.Z);
                default:
                    return C;
            }
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

        private bool CanPlace(VoxelHandle Location, Rail.JunctionPiece Piece, RailEntity PreviewEntity)
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
                if ((entity as GameComponent).IsDead)
                    continue;

                if (Object.ReferenceEquals(entity, PreviewEntity)) continue;
                if (entity is GenericVoxelListener) continue;
                if (entity is WorkPile) continue;

                if (FindPossibleCombination(Piece, entity) != null)
                    return true;

                if (Debugger.Switches.DrawBoundingBoxes)
                {
                    Drawer3D.DrawBox(entity.GetBoundingBox(), Color.Yellow, 0.1f, false);
                    Player.World.ShowToolPopup(String.Format("Can't place {0}. Entity in the way: {1}", Piece.RailPiece, entity.ToString()));
                }

                return false;
            }

            return true;
        }

        private static CombinationTable.Combination FindPossibleCombination(Rail.JunctionPiece Piece, IBoundedObject Entity)
        {
            if (Entity is RailEntity)
            {
                var baseJunction = (Entity as RailEntity).GetPiece();
                var basePiece = Rail.RailLibrary.GetRailPiece(baseJunction.RailPiece);
                var relativeOrientation = Rail.OrientationHelper.Relative(baseJunction.Orientation, Piece.Orientation);

                if (basePiece.Name == Piece.RailPiece && relativeOrientation == PieceOrientation.North)
                    return new CombinationTable.Combination
                    {
                        Result = basePiece.Name,
                        ResultRelativeOrientation = PieceOrientation.North
                    };

                var matchingCombination = RailLibrary.CombinationTable.FindCombination(
                    basePiece.Name, Piece.RailPiece, relativeOrientation);
                return matchingCombination;
            }

            return null;
        }

        private bool CanPlace(VoxelHandle Location)
        {
            for (var i = 0; i < PreviewBodies.Count; ++i)
                if (!CanPlace(Location, PreviewBodies[i].GetPiece(), PreviewBodies[i]))
                    return false;
            return true;
        }

        private void Place(VoxelHandle Location)
        {
            var assignments = new List<Task>();

            for (var i = 0; i < PreviewBodies.Count; ++i)
            {
                var body = PreviewBodies[i];
                var piece = body.GetPiece();
                var actualPosition = new VoxelHandle(Location.Chunk.Manager.ChunkData, Location.Coordinate + new GlobalVoxelOffset(piece.Offset.X, 0, piece.Offset.Y));
                var addNewDesignation = true;
                var hasResources = false;
                var finalEntity = body;

                foreach (var entity in Player.World.CollisionManager.EnumerateIntersectingObjects(actualPosition.GetBoundingBox().Expand(-0.2f), CollisionManager.CollisionType.Static))
                {
                    if ((entity as GameComponent).IsDead)
                        continue;

                    if (!addNewDesignation) break;
                    if (Object.ReferenceEquals(entity, body)) continue;

                    var possibleCombination = FindPossibleCombination(piece, entity);
                    if (possibleCombination != null)
                    {
                        var combinedPiece = new Rail.JunctionPiece
                        {
                            RailPiece = possibleCombination.Result,
                            Orientation = Rail.OrientationHelper.Rotate((entity as RailEntity).GetPiece().Orientation, (int)possibleCombination.ResultRelativeOrientation),
                        };

                        var existingDesignation = Player.Faction.Designations.EnumerateEntityDesignations(DesignationType.Craft).FirstOrDefault(d => Object.ReferenceEquals(d.Body, entity));
                        if (existingDesignation != null)
                        {
                            (entity as RailEntity).UpdatePiece(combinedPiece, actualPosition);
                            (existingDesignation.Tag as CraftDesignation).Progress = 0.0f;
                            body.Delete();
                            addNewDesignation = false;
                            finalEntity = entity as RailEntity;
                        }
                        else
                        {
                            (entity as RailEntity).Delete();
                            body.UpdatePiece(combinedPiece, actualPosition);
                            hasResources = true;
                        }
                    }
                }

                if (!GodModeSwitch && addNewDesignation)
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
                        HasResources = hasResources,
                        ResourcesReservedFor = null,
                        Orientation = 0.0f,
                        Progress = 0.0f,
                    };
                    body.SetFlag(GameComponent.Flag.ShouldSerialize, true);

                    Player.World.ComponentManager.RootComponent.AddChild(designation.WorkPile);
                    designation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), endPos));
                    Player.World.ParticleManager.Trigger("puff", endPos, Color.White, 10);
                    Player.Faction.Designations.AddEntityDesignation(body, DesignationType.Craft, designation);
                    assignments.Add(new CraftItemTask(designation));
                }

                if (GodModeSwitch)
                {
                    // Go ahead and activate the entity and destroy the designation and workpile.
                    var existingDesignation = Player.Faction.Designations.EnumerateEntityDesignations(DesignationType.Craft).FirstOrDefault(d => Object.ReferenceEquals(d.Body, finalEntity));
                    if (existingDesignation != null)
                    {
                        var designation = existingDesignation.Tag as CraftDesignation;
                        if (designation != null && designation.WorkPile != null)
                            designation.WorkPile.Delete();
                        Player.Faction.Designations.RemoveEntityDesignation(finalEntity, DesignationType.Craft);
                    }

                    finalEntity.SetFlagRecursive(GameComponent.Flag.Active, true);
                    finalEntity.SetTintRecursive(Color.White);
                    finalEntity.SetFlagRecursive(GameComponent.Flag.Visible, true);
                    foreach (var tinter in finalEntity.EnumerateAll().OfType<Tinter>())
                        tinter.Stipple = false;
                }
            }

            if (!GodModeSwitch && assignments.Count > 0)
                Player.World.Master.TaskManager.AddTasks(assignments);
        }
    }
}
