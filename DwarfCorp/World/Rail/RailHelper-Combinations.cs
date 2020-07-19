using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Rail
{
    public partial class RailHelper
    {
        public static RailEntity CreatePreviewBody(ComponentManager Manager, VoxelHandle Location, JunctionPiece Piece)
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

        public static bool CanPlace(WorldManager World, List<RailEntity> PreviewBodies)
        {
            for (var i = 0; i < PreviewBodies.Count; ++i)
            {
                PreviewBodies[i].PropogateTransforms();
                if (!RailHelper.CanPlace(World, PreviewBodies[i]))
                    return false;
            }
            return true;
        }

        public static bool CanPlace(WorldManager World, RailEntity PreviewEntity)
        {
            // Todo: Make sure this uses BuildObjectTool.IsValidPlacement to enforce building rules.

            var junctionPiece = PreviewEntity.GetPiece();
            var actualPosition = PreviewEntity.GetContainingVoxel();
            if (!actualPosition.IsValid) return false;
            if (!actualPosition.IsEmpty) return false;

            if (actualPosition.Coordinate.Y == 0) return false; // ???

            var voxelUnder = VoxelHelpers.GetVoxelBelow(actualPosition);
            if (voxelUnder.IsEmpty) return false;
            var box = actualPosition.GetBoundingBox().Expand(-0.2f);

            foreach (var entity in World.EnumerateIntersectingRootObjects(box, CollisionType.Static))
            {
                if (entity.IsDead)
                    continue;

                if (Object.ReferenceEquals(entity, PreviewEntity)) continue;
                if (Object.ReferenceEquals(entity.GetRoot(), PreviewEntity.GetRoot())) continue;
                if (entity is GenericVoxelListener) continue; // Are these ever true now?
                if (entity is WorkPile) continue;
                if (entity is Health) continue;
                if (entity is CraftDetails) continue;
                if (entity is SimpleSprite) continue;

                if (FindPossibleCombination(junctionPiece, entity).HasValue(out var possibleCombination))
                {
                    var combinedPiece = new Rail.JunctionPiece
                    {
                        RailPiece = possibleCombination.Result,
                        Orientation = Rail.OrientationHelper.Rotate((entity as RailEntity).GetPiece().Orientation, (int)possibleCombination.ResultRelativeOrientation),
                    };

                    PreviewEntity.UpdatePiece(combinedPiece, PreviewEntity.GetContainingVoxel());
                    return true;
                }

                if (Debugger.Switches.DrawToolDebugInfo)
                    Drawer3D.DrawBox(box, Color.Yellow, 0.2f, false);

                World.UserInterface.ShowTooltip(String.Format("Can't place {0}. Entity in the way: {1}", junctionPiece.RailPiece, entity.ToString()));
                return false;
            }

            return true;
        }

        public static MaybeNull<CombinationTable.Combination> FindPossibleCombination(Rail.JunctionPiece Piece, GameComponent Entity)
        {
            if (Entity is RailEntity)
            {
                var baseJunction = (Entity as RailEntity).GetPiece();
                if (Library.GetRailPiece(baseJunction.RailPiece).HasValue(out var basePiece))
                {
                    var relativeOrientation = Rail.OrientationHelper.Relative(baseJunction.Orientation, Piece.Orientation);

                    if (basePiece.Name == Piece.RailPiece && relativeOrientation == PieceOrientation.North)
                        return new CombinationTable.Combination
                        {
                            Result = basePiece.Name,
                            ResultRelativeOrientation = PieceOrientation.North
                        };

                    var matchingCombination = Library.FindRailCombination(basePiece.Name, Piece.RailPiece, relativeOrientation);
                    return matchingCombination;
                }
            }

            return null;
        }

        public static void Place(WorldManager World, List<RailEntity> PreviewBodies, bool GodModeSwitch)
        {
            // Assume CanPlace was called and returned true.

            var assignments = new List<Task>();

            for (var i = 0; i < PreviewBodies.Count; ++i)
            {
                var body = PreviewBodies[i];
                var piece = body.GetPiece();
                var actualPosition = body.GetContainingVoxel();
                var addNewDesignation = true;
                var hasResources = false;
                var finalEntity = body;

                foreach (var entity in World.EnumerateIntersectingRootObjects(actualPosition.GetBoundingBox().Expand(-0.2f), CollisionType.Static))
                {
                    if (entity.IsDead)
                        continue;
                    if ((entity as RailEntity) == null)
                        continue;

                    if (!addNewDesignation) break;
                    if (Object.ReferenceEquals(entity, body)) continue;

                    var existingDesignation = World.PersistentData.Designations.EnumerateEntityDesignations(DesignationType.PlaceObject).FirstOrDefault(d => Object.ReferenceEquals(d.Body, entity));
                    if (existingDesignation != null)
                    {
                        (entity as RailEntity).UpdatePiece(piece, actualPosition);
                        (existingDesignation.Tag as PlacementDesignation).Progress = 0.0f;
                        body.GetRoot().Delete();
                        addNewDesignation = false;
                        finalEntity = entity as RailEntity;
                    }
                    else
                    {
                        (entity as RailEntity).GetRoot().Delete();
                        hasResources = true;
                    }

                }

                if (!GodModeSwitch && addNewDesignation)
                {
                    var startPos = body.Position + new Vector3(0.0f, -0.3f, 0.0f);
                    var endPos = body.Position;
                    ResourceType railRes = null;
                    Library.GetResourceType("Rail").HasValue(out railRes); // Todo: Actually check if it's null.
                   
                    var designation = new PlacementDesignation
                    {
                        Entity = body,
                        WorkPile = new WorkPile(World.ComponentManager, startPos),
                        OverrideOrientation = false,
                        ItemType = railRes,
                        Location = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(body.Position)),
                        HasResources = hasResources,
                        Orientation = 0.0f,
                        Progress = 0.0f,
                    };

                    body.SetFlag(GameComponent.Flag.ShouldSerialize, true);
                    World.ComponentManager.RootComponent.AddChild(designation.WorkPile);
                    designation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), endPos));
                    World.ParticleManager.Trigger("puff", endPos, Color.White, 10);

                    var task = new PlaceObjectTask(designation);
                    World.PersistentData.Designations.AddEntityDesignation(body, DesignationType.PlaceObject, designation, task);
                    assignments.Add(task);
                }

                if (GodModeSwitch)
                {
                    // Go ahead and activate the entity and destroy the designation and workpile.
                    var existingDesignation = World.PersistentData.Designations.EnumerateEntityDesignations(DesignationType.PlaceObject).FirstOrDefault(d => Object.ReferenceEquals(d.Body, finalEntity));
                    if (existingDesignation != null)
                    {
                        var designation = existingDesignation.Tag as PlacementDesignation;
                        if (designation != null && designation.WorkPile != null)
                            designation.WorkPile.GetRoot().Delete();
                        World.PersistentData.Designations.RemoveEntityDesignation(finalEntity, DesignationType.PlaceObject);
                    }

                    finalEntity.SetFlagRecursive(GameComponent.Flag.Active, true);
                    finalEntity.SetVertexColorRecursive(Color.White);
                    finalEntity.SetFlagRecursive(GameComponent.Flag.Visible, true);
                    finalEntity.SetFlag(GameComponent.Flag.ShouldSerialize, true);
                    World.PlayerFaction.OwnedObjects.Add(finalEntity);
                    foreach (var tinter in finalEntity.EnumerateAll().OfType<Tinter>())
                        tinter.Stipple = false;
                }
            }

            if (!GodModeSwitch && assignments.Count > 0)
                World.TaskManager.AddTasks(assignments);
        }
    }
}
