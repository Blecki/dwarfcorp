using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    public partial class WorldManager
    {
        public void CreateInitialEmbarkment(Generation.GeneratorSettings Settings)
        {
            // If no file exists, we have to create the balloon and balloon port.
            if (!string.IsNullOrEmpty(Settings.OverworldSettings.InstanceSettings.ExistingFile)) return; // Todo: Don't call in the first place??
             
            var port = GenerateInitialBalloonPort(Renderer.Camera.Position.X, Renderer.Camera.Position.Z, 1, Settings);
            PlayerFaction.Economy.Funds = Settings.OverworldSettings.InstanceSettings.InitalEmbarkment.Funds;
            Settings.OverworldSettings.PlayerCorporationFunds -= Settings.OverworldSettings.InstanceSettings.InitalEmbarkment.Funds;

            foreach (var res in Settings.OverworldSettings.InstanceSettings.InitalEmbarkment.Resources.Enumerate())
            {
                AddResources(res);
                Settings.OverworldSettings.PlayerCorporationResources.Remove(res);
            }

            var portBox = port.GetBoundingBox();

            DoLazy(new Action(() =>
            {
                ComponentManager.RootComponent.AddChild(Balloon.CreateBalloon(
                    portBox.Center() + new Vector3(0, 100, 0),
                    portBox.Center() + new Vector3(0, 10, 0), ComponentManager,
                    PlayerFaction));

                foreach (var applicant in Settings.OverworldSettings.InstanceSettings.InitalEmbarkment.Employees)
                {
                    Settings.OverworldSettings.PlayerCorporationFunds -= applicant.SigningBonus;
                    HireImmediately(applicant);
                }
            }));

            Renderer.Camera.Target = portBox.Center();
            Renderer.Camera.Position = Renderer.Camera.Target + new Vector3(0, 15, -15);
        }

        public static Zone GenerateInitialBalloonPort(float x, float z, int size, Generation.GeneratorSettings Settings)
        {
            var roomVoxels = Generation.Generator.GenerateBalloonPort(Settings.World.ChunkManager, x, z, size, Settings);

            // Actually create the BuildRoom.
            var toBuild = RoomLibrary.CreateRoom(Settings.World.PlayerFaction, "Balloon Port", Settings.World); // Todo: Trim redundant parameters
            Settings.World.RoomBuilder.AddZone(toBuild);
            RoomLibrary.CompleteRoomImmediately(toBuild, roomVoxels.StockpileVoxels);

            return toBuild;
        }

    }
}
