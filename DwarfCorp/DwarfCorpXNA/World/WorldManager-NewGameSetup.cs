// PlayState.cs
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
using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    /// <summary>
    /// This is the main game state for actually playing the game.
    /// </summary>
    public partial class WorldManager
    {
       
        /// <summary>
        /// Generates a random set of dwarves in the given chunk.
        /// </summary>
        public void CreateInitialDwarves(Vector3 SpawnPos)
        {
            if (InitialEmbark == null)
                InitialEmbark = EmbarkmentLibrary.DefaultEmbarkment;

            foreach (string ent in InitialEmbark.Party)
            {
                Physics creat = (Physics)EntityFactory.CreateEntity<Physics>(ent, SpawnPos);
                creat.Velocity = new Vector3(1, 0, 0);
            }
        }

        /// <summary>
        /// Creates the balloon, the dwarves, and the initial balloon port.
        /// </summary>
        public void CreateInitialEmbarkment()
        {
            // If no file exists, we have to create the balloon and balloon port.
            if (!string.IsNullOrEmpty(ExistingFile)) return;

            var port = GenerateInitialBalloonPort(Master.Faction.RoomBuilder, ChunkManager, Camera.Position.X, Camera.Position.Z, 3);
            PlayerFaction.AddMoney(InitialEmbark.Money);

            foreach (var res in InitialEmbark.Resources)
                PlayerFaction.AddResources(new ResourceAmount(res.Key, res.Value));

            var portBox = port.GetBoundingBox();

            if (StartUnderground)
            {
                Master.MaxViewingLevel = (int)(port.GetBoundingBox().Max.Y + 1);
                CreateInitialDwarves(port.GetBoundingBox().Center() + new Vector3(0, 2.0f, 0));
            }
            else
            {
                ComponentManager.RootComponent.AddChild(Balloon.CreateBalloon(
                    portBox.Center() + new Vector3(0, 100, 0),
                    portBox.Center() + new Vector3(0, 10, 0), ComponentManager,
                    new ShipmentOrder(0, null), Master.Faction));
                CreateInitialDwarves(port.GetBoundingBox().Center() + new Vector3(0, 10.0f, 0));
            }

            Camera.Target = portBox.Center();
            Camera.Position = Camera.Target + new Vector3(0, 15, -15);

            GenerateInitialObjects();
        }

        private void GenerateInitialObjects()
        {
            float maxHeight = Overworld.GetMaxHeight(SpawnRect);
            Dictionary<string, Dictionary<string, int>> creatures = new Dictionary<string, Dictionary<string, int>>();
            foreach (var chunk in ChunkManager.ChunkData.GetChunkEnumerator())
                ChunkManager.ChunkGen.GenerateSurfaceLife(creatures, chunk, maxHeight);
        }

        /// <summary>
        /// Creates a flat, wooden balloon port for the balloon to land on, and Dwarves to sit on.
        /// </summary>
        /// <param name="roomDes">The player's BuildRoom designator (so that we can create a balloon port)</param>
        /// <param name="chunkManager">The terrain handler</param>
        /// <param name="x">The position of the center of the balloon port</param>
        /// <param name="z">The position of the center of the balloon port</param>
        /// <param name="size">The size of the (square) balloon port in voxels on a side</param>
        public Room GenerateInitialBalloonPort(RoomBuilder roomDes, ChunkManager chunkManager, float x, float z,
            int size)
        {
            var centerCoordinate = GlobalVoxelCoordinate.FromVector3(new Vector3(x, VoxelConstants.ChunkSizeY - 1, z));

            var accumulator = 0;
            var count = 0;
            for (var offsetX = -size; offsetX <= size; ++offsetX)
            {
                for (var offsetY = -size; offsetY <= size; ++offsetY)
                {
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelowIncludeWater(
                        new VoxelHandle(chunkManager.ChunkData,
                            centerCoordinate + new GlobalVoxelOffset(offsetX, 0, offsetY)));

                    if (topVoxel.Coordinate.Y > 0)
                    {
                        accumulator += topVoxel.Coordinate.Y + 1;
                        count += 1;
                    }
                }
            }

            var averageHeight = (int)Math.Round(((float)accumulator / (float)count));

            if (StartUnderground)
            {
                accumulator = 0;
                count = 0;
                List<string> illegalTypes = new List<string>(){ "Sand", "Dirt", "DarkDirt", "Ice" };
                for (var offsetX = -size; offsetX <= size; ++offsetX)
                {
                    for (var offsetY = -size; offsetY <= size; ++offsetY)
                    {
                        var topVoxel = VoxelHelpers.FindFirstVoxelBelow(
                            new VoxelHandle(chunkManager.ChunkData,
                                centerCoordinate + new GlobalVoxelOffset(offsetX, 0, offsetY)));
                       
                        if (topVoxel.Coordinate.Y > 0)
                        {
                            var vox = topVoxel;
                            for (int dy = topVoxel.Coordinate.Y; dy > 0; dy--)
                            {
                                vox = new VoxelHandle(chunkManager.ChunkData, new GlobalVoxelCoordinate(topVoxel.Coordinate.X, dy, topVoxel.Coordinate.Z));
                                if (vox.IsValid && !vox.IsEmpty && !illegalTypes.Contains(vox.Type.Name))
                                {
                                    break;
                                }
                            }
                            accumulator += vox.Coordinate.Y + 1;
                            count += 1;
                        }

                    }
                }
                averageHeight = Math.Max((int)Math.Round(((float)accumulator / (float)count)) - 5, 0);
            }

            // Next, create the balloon port by deciding which voxels to fill.
            var balloonPortDesignations = new List<VoxelHandle>();
            var treasuryDesignations = new List<VoxelHandle>();
            for (int dx = -size; dx <= size; dx++)
            {
                for (int dz = -size; dz <= size; dz++)
                {
                    Vector3 worldPos = new Vector3(centerCoordinate.X + dx, centerCoordinate.Y, centerCoordinate.Z + dz);

                    var baseVoxel = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                        chunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(worldPos)));

                    if (!baseVoxel.IsValid)
                        continue;

                    var h = baseVoxel.Coordinate.Y + 1;
                    var localCoord = baseVoxel.Coordinate.GetLocalVoxelCoordinate();

                    for (int y = averageHeight; y < (StartUnderground ? averageHeight + 2 : h); y++)
                    {
                        var v = new VoxelHandle(baseVoxel.Chunk,
                            new LocalVoxelCoordinate((int)localCoord.X, y, (int)localCoord.Z));
                        v.RawSetType(VoxelLibrary.GetVoxelType(0));
                        v.RawSetIsExplored();
                        v.QuickSetLiquid(LiquidType.None, 0);
                    }

                    if (averageHeight < h)
                    {
                        h = averageHeight;
                    }

                    bool isPosX = (dx == size && dz == 0);
                    bool isPosZ = (dz == size & dx == 0);
                    bool isNegX = (dx == -size && dz == 0);
                    bool isNegZ = (dz == -size && dz == 0);
                    bool isSide = (isPosX || isNegX || isPosZ || isNegZ);

                    Vector3 offset = Vector3.Zero;

                    if (isSide)
                    {
                        if (isPosX)
                        {
                            offset = Vector3.UnitX;
                        }
                        else if (isPosZ)
                        {
                            offset = Vector3.UnitZ;
                        }
                        else if (isNegX)
                        {
                            offset = -Vector3.UnitX;
                        }
                        else if (isNegZ)
                        {
                            offset = -Vector3.UnitZ;
                        }
                    }

                    bool encounteredFilled = false;
                    // Fill from the top height down to the bottom.
                    for (int y = Math.Min(0, h - 1); y < averageHeight && y < VoxelConstants.ChunkSizeY; y++)
                    {
                        var v = new VoxelHandle(baseVoxel.Chunk, 
                            new LocalVoxelCoordinate((int)localCoord.X, y, (int)localCoord.Z));
                        if (!v.IsValid) throw new InvalidProgramException("Voxel was invalid while creating a new game's initial zones. This should not happen.");

                        v.RawSetType(VoxelLibrary.GetVoxelType("Scaffold"));
                        v.IsPlayerBuilt = true;
                        v.QuickSetLiquid(LiquidType.None, 0);

                        if (y == averageHeight - 1)
                        {
                            v.RawSetIsExplored();

                            if (dz >= 0)
                            {
                                balloonPortDesignations.Add(v);
                            }
                            else
                            {
                                treasuryDesignations.Add(v);
                            }
                        }

                        if (isSide && !encounteredFilled)
                        {
                            var ladderPos = new Vector3(worldPos.X, y, worldPos.Z) + offset +
                                Vector3.One * 0.5f;
                            var ladderVox = new VoxelHandle(chunkManager.ChunkData,
                                GlobalVoxelCoordinate.FromVector3(ladderPos));
                            if (ladderVox.IsValid && ladderVox.IsEmpty)
                            {
                                var ladder = EntityFactory.CreateEntity<Ladder>("Ladder", ladderPos);
                                Master.Faction.OwnedObjects.Add(ladder);
                                ladder.Tags.Add("Moveable");
                                ladder.Tags.Add("Deconstructable");
                            }
                            else
                            {
                                encounteredFilled = true;
                            }

                        }
                    }
                }
            }

            // Actually create the BuildRoom.
            var toBuild = RoomLibrary.CreateRoom(PlayerFaction, "Balloon Port", this);
            roomDes.DesignatedRooms.Add(toBuild);
            RoomLibrary.CompleteRoomImmediately(toBuild, balloonPortDesignations);

            // Also add a treasury
            var treasury = RoomLibrary.CreateRoom(PlayerFaction, "Treasury", this);
            roomDes.DesignatedRooms.Add(treasury);
            RoomLibrary.CompleteRoomImmediately(treasury, treasuryDesignations);

            return toBuild;
        }

    }
}
