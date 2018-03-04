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
        private bool Dragging = false;
        private VoxelHandle DragStartVoxel = VoxelHandle.InvalidHandle;

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
            if (!Dragging)
            {
                if (button == InputManager.MouseButton.Left)
                    if (CanPlace(Player.VoxSelector.VoxelUnderMouse))
                    {
                        Place(Player.VoxSelector.VoxelUnderMouse);
                        PreviewBodies.Clear();
                        CreatePreviewBodies(Player.World.ComponentManager, Player.VoxSelector.VoxelUnderMouse);
                    }
            }
            else
            {
                if (button == InputManager.MouseButton.Left)
                {
                    if (CanPlace(DragStartVoxel))
                    {
                        Place(DragStartVoxel);
                        PreviewBodies.Clear();
                        CreatePreviewBodies(Player.World.ComponentManager, Player.VoxSelector.VoxelUnderMouse);
                    }
                    else
                    {
                        CreatePreviewBodies(Player.World.ComponentManager, Player.VoxSelector.VoxelUnderMouse);
                    }

                    Dragging = false;
                }
            }
        }

        public override void OnBegin()
        {
            System.Diagnostics.Debug.Assert(Pattern != null);
            System.Diagnostics.Debug.Assert(SelectedResources != null);
            GodModeSwitch = false;
            Dragging = false;
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
                Pattern = Pattern.Rotate(Rail.Orientation.East);
            if (RightPressed && !rightKey)
                Pattern = Pattern.Rotate(Rail.Orientation.West);
            LeftPressed = leftKey;
            RightPressed = rightKey;

            var tint = Color.White;

            if (!Dragging)
            {
                var voxelUnderMouse = Player.VoxSelector.VoxelUnderMouse;
                UpdatePreviewBodies(voxelUnderMouse);

                if (CanPlace(voxelUnderMouse))
                    tint = Color.Green;
                else
                    tint = Color.Red;
            }
            else
            {
                var voxelUnderMouse = Player.VoxSelector.VoxelUnderMouse;
                if (voxelUnderMouse == DragStartVoxel)
                {
                    if (PreviewBodies.Count > Pattern.Pieces.Count)
                        CreatePreviewBodies(Player.World.ComponentManager, voxelUnderMouse);
                    UpdatePreviewBodies(voxelUnderMouse);

                    if (CanPlace(voxelUnderMouse))
                        tint = Color.Green;
                    else
                        tint = Color.Red;
                }
                else
                {
                    PreviewBodies[0].UpdatePiece(Pattern.Pieces[0], DragStartVoxel);

                    var bodyCounter = 1;
                    var destinationPoint = voxelUnderMouse.Coordinate.ToVector3();
                    JunctionPortal lastExit = null;
                    var currentVoxel = DragStartVoxel.Coordinate;

                    // Determine which end of start is closer to destination.
                    var entranceCoordinate = OffsetCoordinateThroughPortal(DragStartVoxel.Coordinate + new GlobalVoxelOffset(Pattern.Entrance.Offset.X, 0, Pattern.Entrance.Offset.Y), Pattern.Entrance);
                    var exitCoordinate = OffsetCoordinateThroughPortal(DragStartVoxel.Coordinate + new GlobalVoxelOffset(Pattern.Exit.Offset.X, 0, Pattern.Exit.Offset.Y), Pattern.Exit);

                    if ((destinationPoint - entranceCoordinate.ToVector3()).LengthSquared() < (destinationPoint - exitCoordinate.ToVector3()).LengthSquared())
                    {
                        currentVoxel = entranceCoordinate;
                        lastExit = Pattern.Entrance;
                    }
                    else
                    {
                        currentVoxel = exitCoordinate;
                        lastExit = Pattern.Exit;
                    }

                    var currentPoint = DragStartVoxel.Coordinate.ToVector3() + new Vector3(lastExit.Offset.X, 0.0f, lastExit.Offset.Y);
                    
                    while (true)
                    {
                        var nextPiece = RailLibrary.EnumerateChainPatterns()
                            .Where(p => p.Entrance.Direction == OrientationHelper.Rotate(lastExit.Direction, 2)) // Only consider pieces that line up.
                            .OrderBy(p =>
                            {
                                var exitVoxel = OffsetCoordinateThroughPortal(currentVoxel + new GlobalVoxelOffset(p.Exit.Offset.X, 0, p.Exit.Offset.Y), p.Exit);
                                return (destinationPoint - exitVoxel.ToVector3()).LengthSquared();
                            })
                            .First();

                        var exitVoxelPoint = OffsetCoordinateThroughPortal(currentVoxel + new GlobalVoxelOffset(nextPiece.Exit.Offset.X, 0, nextPiece.Exit.Offset.Y), nextPiece.Exit);

                        if ((destinationPoint - exitVoxelPoint.ToVector3()).LengthSquared() > (destinationPoint - currentPoint).LengthSquared()) break;

                        var patternOffset = new Point(currentVoxel.X - DragStartVoxel.Coordinate.X, currentVoxel.Z - DragStartVoxel.Coordinate.Z);

                        // Add the next pattern to the chain
                        foreach (var piece in nextPiece.Pieces)
                        {
                            var newPiece = new JunctionPiece
                            {
                                Offset = new Point(piece.Offset.X + patternOffset.X, piece.Offset.Y + patternOffset.Y),
                                RailPiece = piece.RailPiece,
                                Orientation = piece.Orientation
                            };

                            if (PreviewBodies.Count <= bodyCounter)
                                PreviewBodies.Add(CreatePreviewBody(Player.World.ComponentManager, DragStartVoxel, newPiece));
                            else
                                PreviewBodies[bodyCounter].UpdatePiece(newPiece, DragStartVoxel);

                            bodyCounter += 1;
                        }

                        currentVoxel = OffsetCoordinateThroughPortal(currentVoxel + new GlobalVoxelOffset(nextPiece.Exit.Offset.X, 0, nextPiece.Exit.Offset.Y), nextPiece.Exit);
                        currentPoint = exitVoxelPoint.ToVector3();
                        lastExit = nextPiece.Exit;
                    }

                    // Clean up any excess preview entities.
                    var lineSize = bodyCounter;

                    while (bodyCounter < PreviewBodies.Count)
                    {
                        PreviewBodies[bodyCounter].Delete();
                        bodyCounter += 1;
                    }

                    PreviewBodies = PreviewBodies.Take(lineSize).ToList();

                    // Apply tint
                    if (CanPlace(DragStartVoxel))
                        tint = Color.Green;
                    else
                        tint = Color.Red;
                }
            }

            foreach (var body in PreviewBodies)
            {
                body.SetTintRecursive(tint);
                var tinters = body.EnumerateAll().OfType<Tinter>();
                foreach(var tinter in tinters)
                {
                    tinter.Stipple = true;
                }
            }
        }

        private GlobalVoxelCoordinate OffsetCoordinateThroughPortal(GlobalVoxelCoordinate C, JunctionPortal Portal)
        {
            switch (Portal.Direction)
            {
                case Orientation.North:
                    return new GlobalVoxelCoordinate(C.X, C.Y, C.Z + 1);
                case Orientation.East:
                    return new GlobalVoxelCoordinate(C.X + 1, C.Y, C.Z);
                case Orientation.South:
                    return new GlobalVoxelCoordinate(C.X, C.Y, C.Z - 1);
                case Orientation.West:
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
            if (Pattern.PaintMode == JunctionPaintMode.Path)
            {
                if (!Dragging)
                {
                    Dragging = true;
                    DragStartVoxel = Player.VoxSelector.FirstVoxel;
                }
            }
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

                if (basePiece.Name == Piece.RailPiece && relativeOrientation == Orientation.North)
                    return new CombinationTable.Combination
                    {
                        Result = basePiece.Name,
                        ResultRelativeOrientation = Orientation.North
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
                }
            }

            if (!GodModeSwitch && assignments.Count > 0)
                Player.World.Master.TaskManager.AddTasks(assignments);
        }
    }
}
