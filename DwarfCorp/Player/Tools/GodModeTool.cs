using System.Collections.Generic;
using System.Data;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class GodModeTool : PlayerTool
    {
        [ToolFactory("God")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new GodModeTool(World);
        }

        public String Command;
        public ChunkManager Chunks { get; set; }

        public override void OnBegin(Object Arguments)
        {
            if (Arguments == null)
                throw new InvalidProgramException();

            Command = Arguments.ToString();
        }

        public override void OnEnd()
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public GodModeTool(WorldManager World)
        {
            this.World = World;
            Chunks = World.ChunkManager;
        }

        private VoxelSelectionType GetSelectionTypeBySelectionBoxValue(string arg)
        {
            if (arg == "Delete Block" || arg.Contains("Build") || arg == "Kill Block" || arg.Contains("Decal") || arg.Contains("Grass"))
            {
                return VoxelSelectionType.SelectFilled;
            }
            else if (arg == "Nuke Column")
                return VoxelSelectionType.SelectFilled;
            else
            {
                return VoxelSelectionType.SelectEmpty;
            }
        }

        private void SelectorBox_OnSelectionModified(string arg)
        {
            World.UserInterface.VoxSelector.SelectionType = GetSelectionTypeBySelectionBoxValue(arg);
        }

        public override void OnVoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            if(Command.Contains("Build/"))
            {
                if (Library.CreateZone(Command.Substring(6), World).HasValue(out var zone))
                {
                    World.AddZone(zone);
                    zone.CompleteRoomImmediately(refs);
                }
            }
            if (Command.Contains("Spawn/"))
            {
                string type = Command.Substring(6);
                foreach (var vox in refs.Where(vox => vox.IsValid))
                {
                    if (vox.IsEmpty)
                    {
                        var offset = Vector3.Zero;

                        if (Library.GetCraftable(type).HasValue(out var craftItem))
                            offset = craftItem.SpawnOffset;

                        var body = EntityFactory.CreateEntity<GameComponent>(type, vox.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + offset);
                        if (body != null)
                        {
                            body.PropogateTransforms();

                            if (craftItem != null)
                            {
                                if (craftItem.AddToOwnedPool)
                                    World.PlayerFaction.OwnedObjects.Add(body);

                                if (craftItem.Deconstructable)
                                    body.Tags.Add("Deconstructable");
                            }
                        }
                    }
                }
            }
            else if (Command.Contains("Rail/"))
            {
                string type = Command.Substring("Rail/".Length);
                var junction = new Rail.JunctionPiece
                {
                    RailPiece = type,
                    Orientation = Rail.PieceOrientation.North,
                    Offset = Point.Zero
                };

                foreach (var vox in refs.Where(vox => vox.IsValid))
                {
                    if (vox.IsEmpty)
                    {
                        var entity = new Rail.RailEntity(World.ComponentManager, vox, junction);
                        World.ComponentManager.RootComponent.AddChild(entity);
                    }
                }
            }
            else if (Command.Contains("Grass/"))
            {
                var type = Library.GetGrassType(Command.Substring(6));
                if (type != null)
                {
                    foreach (var vox in refs.Where(v => v.IsValid))
                    {
                        var v = vox;
                        if (!vox.IsEmpty)
                        {
                            v.GrassType = type.ID;
                            v.GrassDecay = type.InitialDecayValue;
                        }
                    }
                }
            }
            else if (Command.Contains("Decal/"))
            {
                var type = Library.GetDecalType(Command.Substring(6));
                if (type != null)
                {
                    foreach (var vox in refs.Where(v => v.IsValid))
                    {
                        var v = vox;
                        if (!vox.IsEmpty)
                        {
                            v.DecalType = type.ID;
                        }
                    }
                }
            }
            else if (Command.Contains("Disease"))
            { 
                foreach (var creature in World.EnumerateIntersectingObjects(VoxelHelpers.GetVoxelBoundingBox(refs), CollisionType.Both).OfType<Creature>())
                    creature.Stats.AcquireDisease(DiseaseLibrary.GetRandomDisease());
            }
            else
            {
                foreach (var vox in refs.Where(vox => vox.IsValid))
                {
                    if (Command.Contains("Place/"))
                    {
                        string type = Command.Substring(6);
                        var v = vox;
                        if (Library.GetVoxelType(type).HasValue(out VoxelType vType))
                            v.Type = vType;
                        v.QuickSetLiquid(LiquidType.None, 0);

                        if (type == "Magic")
                        {
                            World.ComponentManager.RootComponent.AddChild(
                                new DestroyOnTimer(World.ComponentManager, World.ChunkManager, vox)
                                {
                                    DestroyTimer = new Timer(5.0f + MathFunctions.Rand(-0.5f, 0.5f), true)
                                });
                        }
                    }
                    else switch (Command)
                        {
                            case "Delete Block":
                                {
                                    var v = vox;
                                    World.OnVoxelDestroyed(vox);
                                    v.Type = Library.EmptyVoxelType;
                                    v.QuickSetLiquid(LiquidType.None, 0);
                                }
                                break;
                            case "Nuke Column":
                                {
                                    for (var y = 1; y < World.WorldSizeInVoxels.Y; ++y)
                                    {
                                        var v = World.ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(vox.Coordinate.X, y, vox.Coordinate.Z));
                                        v.Type = Library.EmptyVoxelType;
                                        v.QuickSetLiquid(LiquidType.None, 0);
                                    }
                                }
                                break;
                            case "Kill Block":
                                foreach (var selected in refs)
                                {
                                    if (!selected.IsEmpty)
                                        VoxelHelpers.KillVoxel(World, selected);
                                }
                                break;
                            case "Fill Water":
                                {
                                    if (vox.IsEmpty)
                                    {
                                        var v = vox;
                                        v.QuickSetLiquid(LiquidType.Water, WaterManager.maxWaterLevel);
                                    }
                                }
                                break;
                            case "Fill Lava":
                                {
                                    if (vox.IsEmpty)
                                    {
                                        var v = vox;
                                        v.QuickSetLiquid(LiquidType.Lava, WaterManager.maxWaterLevel);
                                    }
                                }
                                break;
                            case "Fire":
                                {
                                    foreach (var flam2 in World.EnumerateIntersectingObjects(vox.GetBoundingBox(), CollisionType.Both).OfType<Flammable>())
                                        flam2.Heat = flam2.Flashpoint + 1;
                                }
                                break;
                                                            
                            default:
                                break;
                        }
                }
            }
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
                return;
            }

            if (Command == "Kill Things")
            {
                World.UserInterface.BodySelector.Enabled = true;
                World.UserInterface.VoxSelector.Enabled = false;
            }
            else
            {
                World.UserInterface.VoxSelector.SelectionType = GetSelectionTypeBySelectionBoxValue(Command);
                World.UserInterface.VoxSelector.Enabled = true;
                World.UserInterface.VoxSelector.DrawBox = true;
                World.UserInterface.VoxSelector.DrawVoxel = true;
                World.UserInterface.BodySelector.Enabled = false;
            }           

            World.UserInterface.SetMouse(World.UserInterface.MousePointer);

            if (Command == "Repulse")
            {
                var location = World.UserInterface.VoxSelector.VoxelUnderMouse;
                var center = location.GetBoundingBox().Center();
                foreach (var body in World.EnumerateIntersectingObjects(location.GetBoundingBox(), CollisionType.Dynamic))
                {
                    var delta = center - body.Position;
                    delta.Normalize();
                    if (delta.Y < 0)
                        delta.Y = 0;
                    var transform = body.LocalTransform;
                    transform.Translation += delta * (float)time.ElapsedGameTime.TotalSeconds * 5;
                    body.LocalTransform = transform;
                }
            }
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }


        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            if (Command.Contains("Kill Things"))
            {
                foreach (var root in bodies.Where(c => c.IsRoot()))
                    root.Die();
            }

        }
    }

}
