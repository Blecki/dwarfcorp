// CraftItemAct.cs
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This Act causes the Creature to seek out resources and then craft an Item or Resource.
    /// </summary>
    [JsonObject(IsReference = true)]
    internal class CraftItemAct : CompoundCreatureAct
    {
        public CraftItemAct()
        {
        }

        public CraftItemAct(CreatureAI creature, CraftItem type) :
            base(creature)
        {
            ItemType = type;
            Voxel = null;
            Name = "Build craft item";
        }

        public CraftItemAct(CreatureAI creature, Voxel voxel, CraftItem type) :
            base(creature)
        {
            ItemType = type;
            Voxel = voxel;
            Name = "Build craft item";
        }

        /// <summary>
        /// Gets or sets the type of the item to craft.
        /// </summary>
        /// <value>
        /// The type of the item.
        /// </value>
        public CraftItem ItemType { get; set; }
        /// <summary>
        /// Gets or sets the voxel to place the crafted item in.
        /// </summary>
        /// <value>
        /// The voxel.
        /// </value>
        public Voxel Voxel { get; set; }

        /// <summary>
        /// Destroys the resources used to craft the item.
        /// </summary>
        /// <returns>Success.</returns>
        public IEnumerable<Status> DestroyResources()
        {
            Creature.Inventory.Remove(ItemType.SelectedResources);
            yield return Status.Success;
        }

        /// <summary>
        /// Creates resources that were crafted during the Act.
        /// </summary>
        /// <returns>Success if the item could be successfully crafted</returns>
        public IEnumerable<Status> CreateResources()
        {
            // Gets the resources the creature has stashed to craft the item.
            var stashed = Agent.Blackboard.GetData<List<ResourceAmount>>("ResourcesStashed");
            ItemType.SelectedResources = stashed;

            // If crafting a trinket, create a new one using the stashed resources.
            if (ItemType.Name == ResourceLibrary.ResourceType.Trinket)
            {
                Resource craft = ResourceLibrary.GenerateTrinket(stashed.ElementAt(0).ResourceType.Type,
                    (Agent.Stats.Dexterity + Agent.Stats.Intelligence)/15.0f*MathFunctions.Rand(0.5f, 1.75f));
                ItemType.ResourceCreated = craft.Type;
            }
            // If creating a meal, combine two edible resources to produce the meal.
            else if (ItemType.Name == ResourceLibrary.ResourceType.Meal)
            {
                if (stashed.Count < 2)
                {
                    yield return Status.Fail;
                    yield break;
                }
                Resource craft = ResourceLibrary.CreateMeal(stashed.ElementAt(0).ResourceType,
                    stashed.ElementAt(1).ResourceType);
                ItemType.ResourceCreated = craft.Type;
            }
            // If brewing Ale, create Ale using the brewable resource.
            else if (ItemType.Name == ResourceLibrary.ResourceType.Ale)
            {
                Resource craft = ResourceLibrary.CreateAle(stashed.ElementAt(0).ResourceType);
                ItemType.ResourceCreated = craft.Type;
            }
            // If baking bread, create it using the bakeable resource.
            else if (ItemType.Name == ResourceLibrary.ResourceType.Bread)
            {
                Resource craft = ResourceLibrary.CreateBread(stashed.ElementAt(0).ResourceType);
                ItemType.ResourceCreated = craft.Type;
            }
            // If putting gems on a trinket, generate a new trinket that
            // has gems on it.
            else if (ItemType.Name == ResourceLibrary.ResourceType.GemTrinket)
            {
                Resource gem = null;
                Resource trinket = null;
                foreach (ResourceAmount stashedResource in stashed)
                {
                    if (stashedResource.ResourceType.Tags.Contains(Resource.ResourceTags.Craft))
                    {
                        trinket = stashedResource.ResourceType;
                    }

                    if (stashedResource.ResourceType.Tags.Contains(Resource.ResourceTags.Gem))
                    {
                        gem = stashedResource.ResourceType;
                    }
                }


                if (gem == null || trinket == null)
                {
                    yield return Status.Fail;
                    yield break;
                }

                Resource craft = ResourceLibrary.EncrustTrinket(trinket, gem.Type);
                ItemType.ResourceCreated = craft.Type;
            }

            // Tada! Created the resource. Add it to the inventory.
            Resource resource = ResourceLibrary.Resources[ItemType.ResourceCreated];
            Creature.Inventory.Resources.AddResource(new ResourceAmount(resource, 1));
            yield return Status.Success;
        }


        public override void Initialize()
        {
            // This act will unreserve the craft location once we're done.
            Act unreserveAct = new Wrap(() => Creature.Unreserve(ItemType.CraftLocation));

            // Amount of time in seconds it will take to craft the item.
            float time = ItemType.BaseCraftTime/Creature.AI.Stats.BuffedInt;
            Act getResources = null;

            // If the item has no selectable resources, then all the resources are required. Go and get them!
            if (ItemType.SelectedResources == null || ItemType.SelectedResources.Count == 0)
            {
                getResources = new GetResourcesAct(Agent, ItemType.RequiredResources);
            }
            // Otherwise, the player specifically requested resources be used to craft. Go and get them!
            else
            {
                getResources = new GetResourcesAct(Agent, ItemType.SelectedResources);
            }

            // This is the special case where the creature is creating an object.
            if (ItemType.Type == CraftItem.CraftType.Object)
            {
                Tree = new Sequence(
                    // First, look for a place to craft the item.
                    new Wrap(() => Creature.FindAndReserve(ItemType.CraftLocation, ItemType.CraftLocation)),
                    // Get the resources needed to craft the item.
                    getResources,
                    // Go to the item that we reserved earlier and start crafting.
                    new Sequence
                        (
                        new GoToTaggedObjectAct(Agent)
                        {
                            Tag = ItemType.CraftLocation,
                            Teleport = false,
                            TeleportOffset = new Vector3(1, 0, 0),
                            ObjectName = ItemType.CraftLocation
                        },
                        // Craft that object!
                        new Wrap(() => Creature.HitAndWait(time, true)),
                        // Destroy resources used during crafting.
                        new Wrap(DestroyResources),
                        // Un-reserve the crafting station.
                        unreserveAct,
                        // Go to the voxel the player told us to put the object at.
                        new GoToVoxelAct(Voxel, PlanAct.PlanType.Adjacent, Agent),
                        // Create the item there.
                        new CreateCraftItemAct(Voxel, Creature.AI, ItemType.Name)
                        // On failure, restock any resources currently in our hands.
                        // If the item was already crafted, but not yet placed, sorry bubs its lost!
                        ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                    ) | new Sequence(unreserveAct, false);
            }
            // This is the special case where the creature is creating a resource.
            else
            {
                Tree = new Sequence(
                    // Locate and reserve a crafting station.
                    new Wrap(() => Creature.FindAndReserve(ItemType.CraftLocation, ItemType.CraftLocation)),
                    // Get the resources required to craft the item.
                    getResources,
                    // Go to the reserved crafting station.
                    new Sequence
                        (
                        new GoToTaggedObjectAct(Agent)
                        {
                            Tag = ItemType.CraftLocation,
                            Teleport = false,
                            TeleportOffset = new Vector3(1, 0, 0),
                            ObjectName = ItemType.CraftLocation
                        },
                        // Craft the item.
                        new Wrap(() => Creature.HitAndWait(time, true)),
                        // Destroy the resources used during crafting.
                        new Wrap(DestroyResources),
                        // Unreserve the crafting station.
                        unreserveAct,
                        // Create the resources that we just crafted.
                        new Wrap(CreateResources),
                        // Put back any excess material not needed for crafting.
                        new Wrap(Creature.RestockAll)
                        // On failure, unreserve the crafting station, and put back what was
                        // in our inventory.
                        ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                    ) | new Sequence(unreserveAct, false);
            }
            base.Initialize();
        }


        public override void OnCanceled()
        {
            // If the Act was canceled, make sure we've unreserved the crafting station.
            foreach (Status statuses in Creature.Unreserve(ItemType.CraftLocation))
            {
            }
            base.OnCanceled();
        }
    }
}