using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    public partial class WorldManager
    {
        public void CreateInitialEmbarkment(Generation.ChunkGeneratorSettings Settings)
        {
            if (GenerateInitialBalloonPort(Settings.Overworld.SpawnPoint.X, Settings.Overworld.SpawnPoint.Y, 1, Settings).HasValue(out var port))
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

            PlayerFaction.Economy.Funds = Settings.Overworld.PlayerCorporationFunds;
            Settings.Overworld.PlayerCorporationFunds = 0;
        }

        public static MaybeNull<Zone> GenerateInitialBalloonPort(float x, float z, int size, Generation.ChunkGeneratorSettings Settings)
        {
            var roomVoxels = Generation.Generator.GenerateBalloonPort(Settings.World.ChunkManager, x, z, size, Settings);

            if (Library.CreateZone("Balloon Port", Settings.World).HasValue(out var zone))
            {
                Settings.World.AddZone(zone);
                zone.CompleteRoomImmediately(roomVoxels.StockpileVoxels);

                var box = zone.GetBoundingBox();
                var at = new Vector3((box.Min.X + box.Max.X) / 2, box.Max.Y + 0.5f, (box.Min.Z + box.Max.Z) / 2);
                var flag = new Flag(Settings.World.ComponentManager, at, Settings.World.PlayerFaction.Economy.Information, new Resource("Flag"));
                Settings.World.PlayerFaction.OwnedObjects.Add(flag);
                flag.Tags.Add("Deconstructable");

                return zone;
            }
            else
                return null;
        }

    }
}
