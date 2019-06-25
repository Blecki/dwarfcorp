using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    public partial class WorldManager
    {
        public void CreateInitialEmbarkment(Generation.ChunkGeneratorSettings Settings)
        {
            // If no file exists, we have to create the balloon and balloon port.
            if (!string.IsNullOrEmpty(Settings.Overworld.InstanceSettings.ExistingFile)) return; // Todo: Don't call in the first place??
             
            var port = GenerateInitialBalloonPort(Renderer.Camera.Position.X, Renderer.Camera.Position.Z, 1, Settings);

            PlayerFaction.Economy.Funds = Settings.Overworld.InstanceSettings.InitalEmbarkment.Funds;
            Settings.Overworld.PlayerCorporationFunds -= Settings.Overworld.InstanceSettings.InitalEmbarkment.Funds;
            Settings.Overworld.PlayerCorporationFunds -= Settings.Overworld.InstanceSettings.CalculateLandValue();

            foreach (var res in Settings.Overworld.InstanceSettings.InitalEmbarkment.Resources.Enumerate())
            {
                AddResources(res);
                Settings.Overworld.PlayerCorporationResources.Remove(res);
            }

            var portBox = port.GetBoundingBox();

            DoLazy(new Action(() =>
            {
                ComponentManager.RootComponent.AddChild(Balloon.CreateBalloon(
                    portBox.Center() + new Vector3(0, 100, 0),
                    portBox.Center() + new Vector3(0, 10, 0), ComponentManager,
                    PlayerFaction));

                foreach (var applicant in Settings.Overworld.InstanceSettings.InitalEmbarkment.Employees)
                {
                    Settings.Overworld.PlayerCorporationFunds -= applicant.SigningBonus;
                    HireImmediately(applicant);
                }
            }));

            Renderer.Camera.Target = portBox.Center();
            Renderer.Camera.Position = Renderer.Camera.Target + new Vector3(0, 15, -15);
        }

        public static Zone GenerateInitialBalloonPort(float x, float z, int size, Generation.ChunkGeneratorSettings Settings)
        {
            var roomVoxels = Generation.Generator.GenerateBalloonPort(Settings.World.ChunkManager, x, z, size, Settings);

            // Actually create the BuildRoom.
            var toBuild = Library.CreateZone("Balloon Port", Settings.World);
            Settings.World.RoomBuilder.AddZone(toBuild);
            toBuild.CompleteRoomImmediately(roomVoxels.StockpileVoxels);

            DoLazy(() =>
            {
                var box = toBuild.GetBoundingBox();
                var at = new Vector3((box.Min.X + box.Max.X - 1) / 2, box.Max.Y, (box.Min.Z + box.Max.Z - 1) / 2);
                var flag = EntityFactory.CreateEntity<Flag>("Flag", at + new Vector3(0.5f, 0.5f, 0.5f));
                Settings.World.PlayerFaction.OwnedObjects.Add(flag);
            });

            return toBuild;
        }

    }
}
