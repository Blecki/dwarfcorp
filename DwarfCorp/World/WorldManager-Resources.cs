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



        public bool RemoveResources(ResourceAmount resources, Vector3 position, Zone stock)
        {
            if (!stock.Resources.HasResource(resources))
                return false;
            if (!(stock is Stockpile))
                return false;

            // Todo: Stockpile deals with it's own boxes.
            var resourceType = ResourceLibrary.GetResourceByName(resources.Type);
            var num = stock.Resources.RemoveMaxResources(resources, resources.Count);

            (stock as Stockpile).HandleBoxes();

            foreach (var tag in resourceType.Tags)
                if (PlayerFaction.CachedResourceTagCounts.ContainsKey(tag)) // Move cache into worldmanager...
                {
                    PlayerFaction.CachedResourceTagCounts[tag] -= num;
                    Trace.Assert(PlayerFaction.CachedResourceTagCounts[tag] >= 0);
                }

            for (int i = 0; i < num; i++)
            {
                GameComponent newEntity = EntityFactory.CreateEntity<GameComponent>(resources.Type + " Resource",
                        (stock as Stockpile).Boxes[(stock as Stockpile).Boxes.Count - 1].LocalTransform.Translation + MathFunctions.RandVector3Cube() * 0.5f);

                TossMotion toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f), 2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, position);
                newEntity.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                newEntity.AnimationQueue.Add(toss);
                toss.OnComplete += () => newEntity.Die();
            }

            PlayerFaction.RecomputeCachedVoxelstate();
            return true;
        }

        public bool RemoveResources(List<ResourceAmount> resources)
        {
            var amounts = new Dictionary<String, ResourceAmount>();

            foreach (ResourceAmount resource in resources)
            {
                if (!amounts.ContainsKey(resource.Type))
                    amounts.Add(resource.Type, new ResourceAmount(resource));
                else
                    amounts[resource.Type].Count += resource.Count;
            }

            if (!HasResources(amounts.Values))
                return false;

            foreach (var resource in resources)
            {
                int count = 0;
                var resourceType = ResourceLibrary.GetResourceByName(resource.Type);
                foreach (var stock in EnumerateZones().Where(s => resources.All(r => s is Stockpile && (s as Stockpile).IsAllowed(r.Type))))
                {
                    int num = stock.Resources.RemoveMaxResources(resource, resource.Count - count);
                    (stock as Stockpile).HandleBoxes();
                    foreach (var tag in resourceType.Tags)
                    {
                        if (PlayerFaction.CachedResourceTagCounts.ContainsKey(tag))
                        {
                            PlayerFaction.CachedResourceTagCounts[tag] -= num;
                            Trace.Assert(PlayerFaction.CachedResourceTagCounts[tag] >= 0);
                        }
                    }

                    count += num;

                    if (count >= resource.Count)
                        break;
                }
            }

            PlayerFaction.RecomputeCachedVoxelstate();
            return true;
        }

        public List<ResourceAmount> GetResourcesWithTags(List<Quantitiy<Resource.ResourceTags>> tags) // Todo: This is only ever called with a list of 1.
        {
            var tagsRequired = new Dictionary<Resource.ResourceTags, int>();
            var tagsGot = new Dictionary<Resource.ResourceTags, int>();
            var amounts = new Dictionary<String, ResourceAmount>();

            foreach (Quantitiy<Resource.ResourceTags> quantity in tags)
            {
                tagsRequired[quantity.Type] = quantity.Count;
                tagsGot[quantity.Type] = 0;
            }

            var r = new Random();

            foreach (var stockpile in EnumerateZones())
                foreach (var resource in stockpile.Resources.Enumerate().OrderBy(x => r.Next()))
                    foreach (var requirement in tagsRequired)
                    {
                        var got = tagsGot[requirement.Key];

                        if (requirement.Value <= got) continue;

                        if (!ResourceLibrary.GetResourceByName(resource.Type).Tags.Contains(requirement.Key)) continue;

                        int amountToRemove = global::System.Math.Min(resource.Count, requirement.Value - got);

                        if (amountToRemove <= 0) continue;

                        tagsGot[requirement.Key] += amountToRemove;

                        if (amounts.ContainsKey(resource.Type))
                        {
                            amounts[resource.Type].Count += amountToRemove;
                        }
                        else
                        {
                            amounts[resource.Type] = new ResourceAmount(resource.Type, amountToRemove);
                        }
                    }

            var toReturn = new List<ResourceAmount>();

            foreach (var requirement in tagsRequired)
            {
                ResourceAmount maxAmount = null;
                foreach (var pair in amounts)
                {
                    if (!ResourceLibrary.GetResourceByName(pair.Key).Tags.Contains(requirement.Key)) continue;
                    if (maxAmount == null || pair.Value.Count > maxAmount.Count)
                    {
                        maxAmount = pair.Value;
                    }
                }
                if (maxAmount != null)
                {
                    toReturn.Add(maxAmount);
                }
            }
            return toReturn;
        }

        public bool HasResources(IEnumerable<Quantitiy<Resource.ResourceTags>> resources)
        {
            foreach (Quantitiy<Resource.ResourceTags> resource in resources)
            {
                int count = EnumerateZones().Sum(stock => stock.Resources.GetResourceCount(resource.Type));

                if (count < resource.Count)
                    return false;
            }

            return true;
        }

        public bool HasResources(IEnumerable<ResourceAmount> resources)
        {
            foreach (ResourceAmount resource in resources)
            {
                int count = EnumerateZones().Sum(stock => stock.Resources.GetResourceCount(resource.Type));

                if (count < resources.Where(r => r.Type == resource.Type).Sum(r => r.Count))
                    return false;
            }

            return true;
        }

        public bool HasResources(String resource)
        {
            return HasResources(new List<ResourceAmount>() { new ResourceAmount(resource) });
        }

        public int CountResourcesWithTag(Resource.ResourceTags tag)
        {
            List<ResourceAmount> resources = ListResourcesWithTag(tag);
            int amounts = 0;

            foreach (ResourceAmount amount in resources)
            {
                amounts += amount.Count;
            }

            return amounts;
        }

        public List<ResourceAmount> ListResourcesWithTag(Resource.ResourceTags tag, bool allowHeterogenous = true)
        {
            var resources = PlayerFaction.ListResources();

            if (allowHeterogenous)
            {
                return (from pair in resources
                        where ResourceLibrary.GetResourceByName(pair.Value.Type).Tags.Contains(tag)
                        select pair.Value).ToList();
            }

            ResourceAmount maxAmount = null;
            foreach (var pair in resources)
            {
                var resource = ResourceLibrary.GetResourceByName(pair.Value.Type);
                if (!resource.Tags.Contains(tag)) continue;
                if (maxAmount == null || pair.Value.Count > maxAmount.Count)
                {
                    maxAmount = pair.Value;
                }
            }
            return maxAmount != null ? new List<ResourceAmount>() { maxAmount } : new List<ResourceAmount>();
        }
    }
}
