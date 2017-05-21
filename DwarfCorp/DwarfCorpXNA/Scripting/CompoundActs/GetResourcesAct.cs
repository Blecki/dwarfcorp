// GetResourcesAct.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item from a stockpile or BuildRoom with the given tags, goes to it, and picks it up.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GetResourcesAct : CompoundCreatureAct
    {
        public List<Quantitiy<Resource.ResourceTags>> Resources { get; set; }
        public List<ResourceAmount> ResourcesToStash { get; set; }
        public GetResourcesAct()
        {

        }

        public GetResourcesAct(CreatureAI agent, List<ResourceAmount> resources) :
            base(agent)
        {
            Name = "Get Resources";
            ResourcesToStash = resources;

        }

        public GetResourcesAct(CreatureAI agent, List<Quantitiy<Resource.ResourceTags>> resources ) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = resources;

        }

        public GetResourcesAct(CreatureAI agent, Resource.ResourceTags resources) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = new List<Quantitiy<Resource.ResourceTags>>(){new Quantitiy<Resource.ResourceTags>(resources)};

        }


        public IEnumerable<Status> AlwaysTrue()
        {
            yield return Status.Success;
        }

        public override void Initialize()
        {

            bool hasAllResources = true;

            if (Resources != null)
            {

                foreach (Quantitiy<Resource.ResourceTags> resource in Resources)
                {

                    if (!Creature.Inventory.Resources.HasResource(resource))
                    {
                        hasAllResources = false;
                    }
                }
            }
            else if (ResourcesToStash != null)
            {
                foreach (ResourceAmount resource in ResourcesToStash)
                {

                    if (!Creature.Inventory.Resources.HasResource(resource))
                    {
                        hasAllResources = false;
                    }
                }
            }


            if(!hasAllResources)
            { 
                Stockpile nearestStockpile = Agent.Faction.GetNearestStockpile(Agent.Position);

                if(ResourcesToStash == null && Resources != null)
                    ResourcesToStash = Agent.Faction.GetResourcesWithTags(Resources);

                if(nearestStockpile == null || ResourcesToStash.Count == 0)
                {
                    if (Resources.Any(r => r.ResourceType == Resource.ResourceTags.Edible) && Agent.Faction == Agent.World.PlayerFaction)
                    {

                        Agent.Manager.World.MakeAnnouncement("We're out of food!");
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
                    }
                    Tree = null;
                    return;
                }
                else
                {
                    Tree = new Sequence(new GoToZoneAct(Agent, nearestStockpile),
                                        new StashResourcesAct(Agent, ResourcesToStash),
                                        new SetBlackboardData<List<ResourceAmount>>(Agent, "ResourcesStashed", ResourcesToStash)
                                        );
                }
            }
            else
            {
                Tree = new SetBlackboardData<List<ResourceAmount>>(Agent, "ResourcesStashed", ResourcesToStash);
            }
          
            base.Initialize();
        }
    }

}