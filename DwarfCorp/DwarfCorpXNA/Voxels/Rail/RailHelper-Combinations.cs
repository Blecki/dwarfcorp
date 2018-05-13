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

        public static bool CanPlace(GameMaster Player, List<RailEntity> PreviewBodies)
        {
            for (var i = 0; i < PreviewBodies.Count; ++i)
                if (!RailHelper.CanPlace(Player, PreviewBodies[i]))
                    return false;
            return true;
        }

        public static bool CanPlace(GameMaster Player, RailEntity PreviewEntity)
        {
            var junctionPiece = PreviewEntity.GetPiece();
            var actualPosition = PreviewEntity.GetContainingVoxel();
            if (!actualPosition.IsValid) return false;
            if (!actualPosition.IsEmpty) return false;

            if (actualPosition.Coordinate.Y == 0) return false; // ???

            var local = actualPosition.Coordinate.GetLocalVoxelCoordinate();
            var voxelUnder = new VoxelHandle(actualPosition.Chunk, new LocalVoxelCoordinate(local.X, local.Y - 1, local.Z));
            if (voxelUnder.IsEmpty) return false;

            foreach (var entity in Player.World.EnumerateIntersectingObjects(actualPosition.GetBoundingBox().Expand(-0.2f), CollisionType.Static))
            {
                if ((entity as GameComponent).IsDead)
                    continue;

                if (Object.ReferenceEquals(entity, PreviewEntity)) continue;
                if (entity is GenericVoxelListener) continue;
                if (entity is WorkPile) continue;

                var possibleCombination = FindPossibleCombination(junctionPiece, entity);
                if (possibleCombination != null)
                {
                    var combinedPiece = new Rail.JunctionPiece
                    {
                        RailPiece = possibleCombination.Result,
                        Orientation = Rail.OrientationHelper.Rotate((entity as RailEntity).GetPiece().Orientation, (int)possibleCombination.ResultRelativeOrientation),
                    };

                    PreviewEntity.UpdatePiece(combinedPiece, PreviewEntity.GetContainingVoxel());
                    return true;
                }

                if (Debugger.Switches.DrawBoundingBoxes)
                {
                    Drawer3D.DrawBox(entity.GetBoundingBox(), Color.Yellow, 0.1f, false);
                    Player.World.ShowToolPopup(String.Format("Can't place {0}. Entity in the way: {1}", junctionPiece.RailPiece, entity.ToString()));
                }

                return false;
            }

            return true;
        }

        public static CombinationTable.Combination FindPossibleCombination(Rail.JunctionPiece Piece, IBoundedObject Entity)
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

        public static void Place(GameMaster Player, List<RailEntity> PreviewBodies, bool GodModeSwitch)
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

                foreach (var entity in Player.World.EnumerateIntersectingObjects(actualPosition.GetBoundingBox().Expand(-0.2f), CollisionType.Static))
                {
                    if ((entity as GameComponent).IsDead)
                        continue;
                    if ((entity as RailEntity) == null)
                        continue;

                    if (!addNewDesignation) break;
                    if (Object.ReferenceEquals(entity, body)) continue;

                    var existingDesignation = Player.Faction.Designations.EnumerateEntityDesignations(DesignationType.Craft).FirstOrDefault(d => Object.ReferenceEquals(d.Body, entity));
                    if (existingDesignation != null)
                    {
                        (entity as RailEntity).UpdatePiece(piece, actualPosition);
                        (existingDesignation.Tag as CraftDesignation).Progress = 0.0f;
                        body.Delete();
                        addNewDesignation = false;
                        finalEntity = entity as RailEntity;
                    }
                    else
                    {
                        (entity as RailEntity).Delete();
                        hasResources = true;
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
                        SelectedResources = new List<ResourceAmount> { new ResourceAmount("Rail", 1) },
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

                    var task = new CraftItemTask(designation);
                    Player.Faction.Designations.AddEntityDesignation(body, DesignationType.Craft, designation, task);
                    assignments.Add(task);
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
                    finalEntity.SetVertexColorRecursive(Color.White);
                    finalEntity.SetFlagRecursive(GameComponent.Flag.Visible, true);
                    finalEntity.SetFlag(GameComponent.Flag.ShouldSerialize, true);
                    Player.Faction.OwnedObjects.Add(finalEntity);
                    foreach (var tinter in finalEntity.EnumerateAll().OfType<Tinter>())
                        tinter.Stipple = false;
                }
            }

            if (!GodModeSwitch && assignments.Count > 0)
                Player.World.Master.TaskManager.AddTasks(assignments);
        }
    }
}
