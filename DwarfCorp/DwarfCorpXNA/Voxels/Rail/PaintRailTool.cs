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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp.Rail
{
    public class PaintRailTool : PlayerTool
    {
        private List<RailEntity> PreviewBodies = new List<RailEntity>();
        private Faction Faction;
        public List<ResourceAmount> SelectedResources;
        public bool GodModeSwitch = false;
        private bool Dragging = false;
        private VoxelHandle DragStartVoxel = VoxelHandle.InvalidHandle;
        private List<GlobalVoxelCoordinate> PathVoxels = new List<GlobalVoxelCoordinate>();
        private bool RightPressed = false;
        private bool LeftPressed = false;
        private CompassOrientation StartingOppositeOrientation = CompassOrientation.North;
        private bool OverrideStartingOrientation = false;
        private CompassOrientation EndingOppositeOrientation = CompassOrientation.North;
        private bool OverrideEndingOrientation = false;


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
            AddToOwnedPool = true,
            Moveable = false            
        };

        public PaintRailTool(GameMaster Player)
        {
            this.Player = Player;
            this.Faction = Player.Faction;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (!Dragging)
            {
            }
            else
            {
                if (button == InputManager.MouseButton.Left)
                {
                    if (CanPlace(DragStartVoxel))
                        Place(DragStartVoxel);
                    else
                        foreach (var piece in PreviewBodies)
                            piece.Delete();

                    PreviewBodies.Clear();
                    PathVoxels.Clear();
                    Dragging = false;
                }
            }
        }

        public override void OnBegin()
        {
            System.Diagnostics.Debug.Assert(SelectedResources != null);
            GodModeSwitch = false;
            Dragging = false;
        }

        public override void OnEnd()
        {
            foreach (var body in PreviewBodies)
                body.Delete();
            PreviewBodies.Clear();
            PathVoxels.Clear();
            SelectedResources = null;
            Player.VoxSelector.DrawVoxel = true;
            Player.VoxSelector.DrawBox = true;
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {

        }

        private RailEntity CreatePreviewBody(ComponentManager Manager, VoxelHandle Location, JunctionPiece Piece)
        {
            var r = new RailEntity(Manager, Location, Piece);
            Manager.RootComponent.AddChild(r);
            r.SetFlagRecursive(GameComponent.Flag.Active, false);

            foreach (var tinter in r.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            //Todo: Add craft details component.
            return r;
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
            Player.VoxSelector.DrawVoxel = true;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            KeyboardState state = Keyboard.GetState();
            bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
            bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
            if (LeftPressed && !leftKey)
            {
                if (PathVoxels.Count > 1)
                {
                    var matched = false;
                    var firstDelta = CompassOrientationHelper.GetVoxelDelta(PathVoxels[0], PathVoxels[1]);
                    var firstConnection = new CompassConnection(OverrideStartingOrientation ? StartingOppositeOrientation : CompassOrientationHelper.Opposite(firstDelta), firstDelta);

                    var orientationDelta = 1;

                    for (; orientationDelta < 8 && !matched; ++orientationDelta)
                    {
                        firstConnection.A = CompassOrientationHelper.Rotate(firstConnection.A, 1);
                        foreach (var piece in RailLibrary.EnumeratePieces().Where(p => p.CompassConnections.Count != 0))
                        {
                            for (int j = 0; j < 4 && !matched; ++j)
                                foreach (var compassConnection in piece.CompassConnections)
                                    if (compassConnection.RotateToPiece((PieceOrientation)j) == firstConnection)
                                        matched = true;
                            if (matched)
                                break;
                        }
                    }

                    if (matched)
                        StartingOppositeOrientation = firstConnection.A;

                    OverrideStartingOrientation = true;
                }
            }
            if (RightPressed && !rightKey)
            {
                if (PathVoxels.Count > 1)
                {
                    var matched = false;
                    var lastDelta = CompassOrientationHelper.GetVoxelDelta(PathVoxels[PathVoxels.Count - 1], PathVoxels[PathVoxels.Count - 2]);
                    var lastConnection = new CompassConnection(lastDelta, OverrideEndingOrientation ? EndingOppositeOrientation : CompassOrientationHelper.Opposite(lastDelta));

                    var orientationDelta = 1;

                    for (; orientationDelta < 8 && !matched; ++orientationDelta)
                    {
                        lastConnection.B = CompassOrientationHelper.Rotate(lastConnection.B, 1);
                        foreach (var piece in RailLibrary.EnumeratePieces().Where(p => p.CompassConnections.Count != 0))
                        {
                            for (int j = 0; j < 4 && !matched; ++j)
                                foreach (var compassConnection in piece.CompassConnections)
                                    if (compassConnection.RotateToPiece((PieceOrientation)j) == lastConnection)
                                        matched = true;
                            if (matched)
                                break;
                        }
                    }

                    if (matched)
                        EndingOppositeOrientation = lastConnection.B;

                    OverrideEndingOrientation = true;
                }
            }
            LeftPressed = leftKey;
            RightPressed = rightKey;

            var tint = Color.White;

            if (!Dragging)
            {
            }
            else
            {
                var voxelUnderMouse = Player.VoxSelector.VoxelUnderMouse;
                if (voxelUnderMouse == DragStartVoxel)
                {
                    // Create single straight preview piece
                    
                }
                else
                {
                    var destinationPoint = voxelUnderMouse.Coordinate;

                    // Prevent path finding from attempting slopes - not supported yet.
                    destinationPoint = new GlobalVoxelCoordinate(destinationPoint.X, DragStartVoxel.Coordinate.Y, destinationPoint.Z);
                    var currentVoxel = DragStartVoxel.Coordinate;

                    PathVoxels.Clear();
                    PathVoxels.Add(currentVoxel);

                    while (true)
                    {
                        var closestDirection = 0;
                        float closestDistance = float.PositiveInfinity;
                        for (var i = 0; i < 8; ++i)
                        {
                            var offsetPos = currentVoxel + CompassOrientationHelper.GetOffset((CompassOrientation)i);
                            var distance = (destinationPoint.ToVector3() - offsetPos.ToVector3()).LengthSquared();
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestDirection = i;
                            }
                        }

                        var nextCoordinate = currentVoxel + CompassOrientationHelper.GetOffset((CompassOrientation)closestDirection);
                        PathVoxels.Add(nextCoordinate);
                        if (PathVoxels.Count >= 100)
                        {
                            break;
                        }

                        if (nextCoordinate == destinationPoint) break;
                        currentVoxel = nextCoordinate;
                    }

                    // Iterate PathVoxels, determining deltas and using them to decide which piece to create.
                    var pathCompassConnections = new List<CompassConnection>();

                    if (PathVoxels.Count > 1)
                    {
                        var firstDelta = CompassOrientationHelper.GetVoxelDelta(PathVoxels[0], PathVoxels[1]);
                        pathCompassConnections.Add(new CompassConnection(OverrideStartingOrientation ? StartingOppositeOrientation : CompassOrientationHelper.Opposite(firstDelta), firstDelta));

                        for (var i = 1; i < PathVoxels.Count - 1; ++i)
                            pathCompassConnections.Add(new CompassConnection(
                                CompassOrientationHelper.GetVoxelDelta(PathVoxels[i], PathVoxels[i - 1]),
                                CompassOrientationHelper.GetVoxelDelta(PathVoxels[i], PathVoxels[i + 1])));

                        var lastDelta = CompassOrientationHelper.GetVoxelDelta(PathVoxels[PathVoxels.Count - 1], PathVoxels[PathVoxels.Count - 2]);
                        pathCompassConnections.Add(new CompassConnection(lastDelta, OverrideEndingOrientation ? EndingOppositeOrientation : CompassOrientationHelper.Opposite(lastDelta)));
                    }

                    var bodyCounter = 0;
                    var previousPieceAddedTrailingDiagonals = false;

                    for (var i = 0; i < pathCompassConnections.Count; ++i)
                    {
                        var pieceAdded = false;

                        foreach (var piece in RailLibrary.EnumeratePieces().Where(p => p.CompassConnections.Count != 0))
                        {
                            var matchedOrientation = PieceOrientation.North;
                            CompassConnection matchedConnection = new CompassConnection();
                            bool matched = false;
                            for (int j = 0; j < 4 && !matched; ++j)
                            {
                                foreach (var compassConnection in piece.CompassConnections)
                                {
                                    var rotated = compassConnection.RotateToPiece((PieceOrientation)j);
                                    if (rotated == pathCompassConnections[i])
                                    {
                                        matched = true;
                                        matchedOrientation = (PieceOrientation)j;
                                        matchedConnection = pathCompassConnections[i];
                                        break;
                                    }
                                }
                            }

                            if (matched)
                            {
                                var newPiece = new JunctionPiece
                                {
                                    Offset = new Point(PathVoxels[i].X - DragStartVoxel.Coordinate.X, PathVoxels[i].Z - DragStartVoxel.Coordinate.Z),
                                    RailPiece = piece.Name,
                                    Orientation = matchedOrientation
                                };

                                if (PreviewBodies.Count <= bodyCounter)
                                   PreviewBodies.Add(CreatePreviewBody(Player.World.ComponentManager, DragStartVoxel, newPiece));
                                else
                                    PreviewBodies[bodyCounter].UpdatePiece(newPiece, DragStartVoxel);

                                bodyCounter += 1;
                                pieceAdded = true;

                                if (!previousPieceAddedTrailingDiagonals &&
                                    (matchedConnection.A == CompassOrientation.Northeast || matchedConnection.A == CompassOrientation.Southeast || matchedConnection.A == CompassOrientation.Southwest
                                    || matchedConnection.A == CompassOrientation.Northwest))
                                {
                                    bodyCounter = AddDiagonal(bodyCounter, matchedConnection.A, newPiece, 7, 5);
                                    bodyCounter = AddDiagonal(bodyCounter, matchedConnection.A, newPiece, 1, 1);
                                }

                                if (matchedConnection.B == CompassOrientation.Northeast || matchedConnection.B == CompassOrientation.Southeast || matchedConnection.B == CompassOrientation.Southwest
                                    || matchedConnection.B == CompassOrientation.Northwest)
                                {
                                    previousPieceAddedTrailingDiagonals = true;

                                    bodyCounter = AddDiagonal(bodyCounter, matchedConnection.B, newPiece, 7, 5);
                                    bodyCounter = AddDiagonal(bodyCounter, matchedConnection.B, newPiece, 1, 1);
                                }
                                else
                                    previousPieceAddedTrailingDiagonals = false;

                                break;
                            }
                        }

                        if (!pieceAdded)
                            break;
                    }
                                        
                    // Clean up any excess preview entities.
                    var lineSize = bodyCounter;

                    while (bodyCounter < PreviewBodies.Count)
                    {
                        PreviewBodies[bodyCounter].Delete();
                        bodyCounter += 1;
                    }

                    PreviewBodies = PreviewBodies.Take(lineSize).ToList();
                }
            }

            if (CanPlace(DragStartVoxel))
                tint = Color.Green;
            else
                tint = Color.Red;

            foreach (var body in PreviewBodies)
                body.SetTintRecursive(tint);
        }

        private int AddDiagonal(int bodyCounter, CompassOrientation B, JunctionPiece newPiece, int CoordinateRotation, int PieceRotation)
        {
            var firstEdgeOffset = CompassOrientationHelper.GetOffset(CompassOrientationHelper.Rotate(B, CoordinateRotation));
            var firstEdgePiece = new JunctionPiece
            {
                Offset = new Point(newPiece.Offset.X + firstEdgeOffset.X, newPiece.Offset.Y + firstEdgeOffset.Z),
                RailPiece = "diag-edge-1",
                Orientation = (PieceOrientation)((int)CompassOrientationHelper.Rotate(B, PieceRotation) / 2)
            };

            if (PreviewBodies.Count <= bodyCounter)
                PreviewBodies.Add(CreatePreviewBody(Player.World.ComponentManager, DragStartVoxel, firstEdgePiece));
            else
                PreviewBodies[bodyCounter].UpdatePiece(firstEdgePiece, DragStartVoxel);

            bodyCounter += 1;
            return bodyCounter;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            for ( var i = 1; i < PathVoxels.Count; ++i)
                Drawer3D.DrawLine(PathVoxels[i - 1].ToVector3() + new Vector3(0.5f, 0.5f, 0.5f), PathVoxels[i].ToVector3() + new Vector3(0.5f, 0.5f, 0.5f), Color.Fuchsia, 0.1f);
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (!Dragging)
            {
                Dragging = true;
                DragStartVoxel = Player.VoxSelector.FirstVoxel;
                StartingOppositeOrientation = CompassOrientation.North;
                OverrideStartingOrientation = false;
                EndingOppositeOrientation = CompassOrientation.North;
                OverrideEndingOrientation = false;
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
                    finalEntity.Tags.Add("Deconstructable");
                    foreach (var tinter in finalEntity.EnumerateAll().OfType<Tinter>())
                        tinter.Stipple = false;
                }
            }

            if (!GodModeSwitch && assignments.Count > 0)
                Player.World.Master.TaskManager.AddTasks(assignments);
        }
    }
}
