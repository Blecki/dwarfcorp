using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using BloomPostprocess;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using Newtonsoft.Json;
using DwarfCorp.Events;
using System.Diagnostics;

namespace DwarfCorp
{
    public partial class WorldManager
    {
        public IEnumerable<Zone> EnumerateZones() // Todo: Belongs to world manager??
        {
            foreach (var room in ZoneBuilder.Zones)
                yield return room;
            yield break;
        }

        public int ComputeRemainingStockpileSpace()
        {
            return EnumerateZones().Where(pile => !(pile is Graveyard)).Sum(pile => pile.Resources.MaxResources - pile.Resources.CurrentResourceCount);
        }

        public int ComputeTotalStockpileSpace()
        {
            return EnumerateZones().Where(pile => !(pile is Graveyard)).Sum(pile => pile.Resources.MaxResources);
        }

        public Zone GetNearestRoomOfType(string typeName, Vector3 position)
        {
            Zone desiredRoom = null;
            float nearestDistance = float.MaxValue;

            foreach (var room in EnumerateZones())
            {
                if (room.Type.Name != typeName || !room.IsBuilt) continue;
                float dist =
                    (room.GetNearestVoxel(position).WorldPosition - position).LengthSquared();

                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    desiredRoom = room;
                }
            }

            return desiredRoom;
        }

        public IEnumerable<KeyValuePair<Zone, ResourceAmount>> GetStockpilesContainingResources(Vector3 biasPos, IEnumerable<ResourceAmount> required)
        {
            foreach (var amount in required)
            {
                var numGot = 0;
                foreach (var stockpile in EnumerateZones().OrderBy(s => (s.GetBoundingBox().Center() - biasPos).LengthSquared()))
                {
                    if (numGot >= amount.Count)
                        break;

                    foreach (var resource in stockpile.Resources.Enumerate().Where(sResource => sResource.Type == amount.Type))
                    {
                        var amountToRemove = System.Math.Min(resource.Count, amount.Count - numGot);
                        if (amountToRemove <= 0)
                            continue;

                        numGot += amountToRemove;
                        yield return new KeyValuePair<Zone, ResourceAmount>(stockpile, new ResourceAmount(resource.Type, amountToRemove));
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<Zone, ResourceAmount>> GetStockpilesContainingResources(List<Quantitiy<Resource.ResourceTags>> tags)
        {
            foreach (var tag in tags)
            {
                int numGot = 0;
                foreach (var stockpile in EnumerateZones())
                {
                    if (numGot >= tag.Count)
                        break;
                    foreach (var resource in stockpile.Resources.Enumerate().Where(sResource => ResourceLibrary.GetResourceByName(sResource.Type).Tags.Contains(tag.Type)))
                    {
                        int amountToRemove = global::System.Math.Min(resource.Count, tag.Count - numGot);
                        if (amountToRemove <= 0) continue;
                        numGot += amountToRemove;
                        yield return new KeyValuePair<Zone, ResourceAmount>(stockpile, new ResourceAmount(resource.Type, amountToRemove));
                    }
                }
            }
        }

        public Zone FindNearestZone(Vector3 position)
        {
            Zone desiredRoom = null;
            float nearestDistance = float.MaxValue;

            foreach (var room in EnumerateZones())
            {
                if (room.Voxels.Count == 0) continue;
                float dist =
                    (room.GetNearestVoxel(position).WorldPosition - position).LengthSquared();

                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    desiredRoom = room;
                }
            }


            return desiredRoom;
        }

        public bool HasFreeStockpile()
        {
            return EnumerateZones().Any(s => s.IsBuilt && !s.IsFull());
        }

        public bool HasFreeStockpile(ResourceAmount toPut)
        {
            return EnumerateZones().Any(s => s.IsBuilt && !s.IsFull() && s is Stockpile && (s as Stockpile).IsAllowed(toPut.Type));
        }

    }
}
