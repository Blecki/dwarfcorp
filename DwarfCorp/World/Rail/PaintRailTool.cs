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
        [ToolFactory("PaintRail")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new PaintRailTool(World);
        }

        private List<RailEntity> PreviewBodies = new List<RailEntity>();
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
        private bool CanPlace = false;
        
        public PaintRailTool(WorldManager World)
        {
            this.World = World;
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
                    if (CanPlace)
                        RailHelper.Place(World, PreviewBodies, GodModeSwitch);
                    else
                        foreach (var piece in PreviewBodies)
                            piece.GetRoot().Delete();

                    PreviewBodies.Clear();
                    PathVoxels.Clear();
                    Dragging = false;
                }
            }
        }

        public override void OnBegin(Object Arguments)
        {
            World.Tutorial("paint rail");
            global::System.Diagnostics.Debug.Assert(SelectedResources != null);
            GodModeSwitch = false;
            Dragging = false;
            CanPlace = false;
        }

        public override void OnEnd()
        {
            foreach (var body in PreviewBodies)
                body.GetRoot().Delete();
            PreviewBodies.Clear();
            PathVoxels.Clear();
            SelectedResources = null;
            World.UserInterface.VoxSelector.DrawVoxel = true;
            World.UserInterface.VoxSelector.DrawBox = true;
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.UserInterface.IsCameraRotationModeActive())
            {
                World.UserInterface.VoxSelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                World.UserInterface.BodySelector.Enabled = false;
                return;
            }

            World.UserInterface.VoxSelector.Enabled = true;
            World.UserInterface.BodySelector.Enabled = false;
            World.UserInterface.VoxSelector.DrawBox = false;
            World.UserInterface.VoxSelector.DrawVoxel = true;
            World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            // Don't attempt any camera control if the user is trying to type intoa focus item.
            if (World.UserInterface.Gui.FocusItem != null && !World.UserInterface.Gui.FocusItem.IsAnyParentTransparent() && !World.UserInterface.Gui.FocusItem.IsAnyParentHidden())
            {
                return;
            }
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
                        foreach (var piece in Library.EnumerateRailPieces().Where(p => p.CompassConnections.Count != 0))
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
                        foreach (var piece in Library.EnumerateRailPieces().Where(p => p.CompassConnections.Count != 0))
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
                var voxelUnderMouse = World.UserInterface.VoxSelector.VoxelUnderMouse;
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

                        foreach (var piece in Library.EnumerateRailPieces().Where(p => p.CompassConnections.Count != 0))
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
                                   PreviewBodies.Add(RailHelper.CreatePreviewBody(World.ComponentManager, DragStartVoxel, newPiece));
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
                        PreviewBodies[bodyCounter].GetRoot().Delete();
                        bodyCounter += 1;
                    }

                    PreviewBodies = PreviewBodies.Take(lineSize).ToList();
                }
            }

            CanPlace = RailHelper.CanPlace(World, PreviewBodies);
            if (CanPlace)
                tint = GameSettings.Default.Colors.GetColor("Positive", Color.Green);
            else
                tint = GameSettings.Default.Colors.GetColor("Negative", Color.Red);

            foreach (var body in PreviewBodies)
                body.SetVertexColorRecursive(tint);
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
                PreviewBodies.Add(RailHelper.CreatePreviewBody(World.ComponentManager, DragStartVoxel, firstEdgePiece));
            else
                PreviewBodies[bodyCounter].UpdatePiece(firstEdgePiece, DragStartVoxel);

            bodyCounter += 1;
            return bodyCounter;
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }


        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (!Dragging)
            {
                Dragging = true;
                DragStartVoxel = World.UserInterface.VoxSelector.FirstVoxel;
                StartingOppositeOrientation = CompassOrientation.North;
                OverrideStartingOrientation = false;
                EndingOppositeOrientation = CompassOrientation.North;
                OverrideEndingOrientation = false;
            }
        }
    }
}
