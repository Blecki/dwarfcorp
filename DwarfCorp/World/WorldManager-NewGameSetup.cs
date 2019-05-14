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
            foreach (string ent in Settings.InitalEmbarkment.Party)
            {
                Physics creat = (Physics)EntityFactory.CreateEntity<Physics>(ent, SpawnPos);
                creat.Velocity = new Vector3(1, 0, 0);
            }
        }

        /// <summary>
        /// Creates the balloon, the dwarves, and the initial balloon port.
        /// </summary>
        public void CreateInitialEmbarkment(Generation.GeneratorSettings Settings)
        {
            // If no file exists, we have to create the balloon and balloon port.
            if (!string.IsNullOrEmpty(Settings.OverworldSettings.ExistingFile)) return;

            var port = GenerateInitialBalloonPort(Master.Faction.RoomBuilder, ChunkManager, Renderer.Camera.Position.X, Renderer.Camera.Position.Z, 1, Settings);
            PlayerFaction.Economy.Funds = Settings.OverworldSettings.InitalEmbarkment.Money;

            foreach (var res in Settings.OverworldSettings.InitalEmbarkment.Resources)
                PlayerFaction.AddResources(new ResourceAmount(res.Key, res.Value));

            var portBox = port.GetBoundingBox();

            DoLazy(new Action(() =>
            {
                ComponentManager.RootComponent.AddChild(Balloon.CreateBalloon(
                    portBox.Center() + new Vector3(0, 100, 0),
                    portBox.Center() + new Vector3(0, 10, 0), ComponentManager,
                    Master.Faction));
                CreateInitialDwarves(port.GetBoundingBox().Center() + new Vector3(0, VoxelConstants.ChunkSizeZ * 0.5f, 0));
            }));

            Renderer.Camera.Target = portBox.Center();
            Renderer.Camera.Position = Renderer.Camera.Target + new Vector3(0, 15, -15);
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
            int size, Generation.GeneratorSettings Settings)
        {
            var roomVoxels = Generation.Generator.GenerateBalloonPort(chunkManager, x, z, size, Settings);

            // Actually create the BuildRoom.
            var toBuild = RoomLibrary.CreateRoom(PlayerFaction, "Balloon Port", this);
            roomDes.DesignatedRooms.Add(toBuild);
            RoomLibrary.CompleteRoomImmediately(toBuild, roomVoxels.StockpileVoxels);

            return toBuild;
        }

    }
}
