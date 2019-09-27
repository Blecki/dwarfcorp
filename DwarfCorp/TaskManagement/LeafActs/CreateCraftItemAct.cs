using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class CreateCraftItemAct : CreatureAct
    {
        public VoxelHandle Voxel { get; set; }
        public CraftDesignation Item { get; set; }

        public CreateCraftItemAct(VoxelHandle voxel, CreatureAI agent, CraftDesignation itemType) :
            base(agent)
        {
            Agent = agent;
            Voxel = voxel;
            Name = "Create craft item";
            Item = itemType;
        }

        public override IEnumerable<Status> Run()
        {
            Item.Finished = true;
            var item = Item.Entity;
            item.SetFlagRecursive(GameComponent.Flag.Active, true);
            item.SetVertexColorRecursive(Color.White);
            var tinters = item.EnumerateAll().OfType<Tinter>();
            foreach(var tinter in tinters)
                tinter.Stipple = false;

            item.SetFlagRecursive(GameComponent.Flag.Visible, true);

            if (Item.ItemType.Deconstructable)
                item.Tags.Add("Deconstructable");

            if (Item.WorkPile != null)
                Item.WorkPile.Die();

            if (item.GetComponent<CraftDetails>().HasValue(out var details))
            {
                details.CraftType = Item.ItemType.Name;
                details.Resources = Item.SelectedResources.ConvertAll(p => p.CloneResource());
            }
            else
            {
                item.AddChild(new CraftDetails(Creature.Manager)
                {
                    Resources = Item.SelectedResources.ConvertAll(p => p.CloneResource()),
                    CraftType = Item.ItemType.Name
                });

                if (Item.SelectedResources.Count > 0)
                    item.Name = Item.SelectedResources.FirstOrDefault().Type + " " + item.Name;
            }

            if (Item.ItemType.AddToOwnedPool)
                Creature.Faction.OwnedObjects.Add(item);

            Creature.Manager.World.ParticleManager.Trigger("puff", Voxel.WorldPosition + Vector3.One * 0.5f, Color.White, 10);
            Creature.AI.AddXP((int)(5 * (Item.ItemType.BaseCraftTime / Creature.AI.Stats.Intelligence)));
            yield return Status.Success;
        }
    }

}