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

            PlayerFaction.Economy.Funds = Settings.Overworld.InstanceSettings.InitalEmbarkment.Funds;
            Settings.Overworld.PlayerCorporationFunds -= Settings.Overworld.InstanceSettings.InitalEmbarkment.Funds;
            Settings.Overworld.PlayerCorporationFunds -= Settings.Overworld.InstanceSettings.CalculateLandValue();

            foreach (var res in Settings.Overworld.InstanceSettings.InitalEmbarkment.Resources.Enumerate())
            {
                AddResources(res);
                Settings.Overworld.PlayerCorporationResources.Remove(res);
            }

            if (GenerateInitialBalloonPort(Renderer.Camera.Position.X, Renderer.Camera.Position.Z, 1, Settings).HasValue(out var port))
            {
                var portBox = port.GetBoundingBox();

                ComponentManager.RootComponent.AddChild(Balloon.CreateBalloon( // Bypassing the entity factory because we need to set the target.
                    portBox.Center() + new Vector3(0, 10, 0),
                    portBox.Center() + new Vector3(0, 10, 0), ComponentManager,
                    PlayerFaction));

                Renderer.Camera.Target = portBox.Center();
                Renderer.Camera.Position = Renderer.Camera.Target + new Vector3(0, 15, -15);
            }

            foreach (var applicant in Settings.Overworld.InstanceSettings.InitalEmbarkment.Employees)
            {
                Settings.Overworld.PlayerCorporationFunds -= applicant.SigningBonus;
                HireImmediately(applicant);
            }
        }

        public static MaybeNull<Zone> GenerateInitialBalloonPort(float x, float z, int size, Generation.ChunkGeneratorSettings Settings)
        {
            var roomVoxels = Generation.Generator.GenerateBalloonPort(Settings.World.ChunkManager, x, z, size, Settings);

            if (Library.CreateZone("Balloon Port", Settings.World).HasValue(out var zone))
            {
                Settings.World.AddZone(zone);
                zone.CompleteRoomImmediately(roomVoxels.StockpileVoxels);

                var box = zone.GetBoundingBox();
                var at = new Vector3((box.Min.X + box.Max.X - 1) / 2, box.Max.Y, (box.Min.Z + box.Max.Z - 1) / 2);
                var flag = EntityFactory.CreateEntity<Flag>("Flag", at + new Vector3(0.5f, 0.5f, 0.5f));
                Settings.World.PlayerFaction.OwnedObjects.Add(flag);

                return zone;
            }
            else
                return null;
        }

    }
}
