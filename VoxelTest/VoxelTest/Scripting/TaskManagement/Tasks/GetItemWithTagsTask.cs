using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should find an item with a tag, and pick it up.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GetItemWithTagsTask : Task
    {
        public TagList Tags = null;

        public GetItemWithTagsTask(TagList tags)
        {
            Tags = tags;
            Name = "Get Item with Tags: " + tags;
        }

        public override Task Clone()
        {
            return new GetItemWithTagsTask(Tags);
        }

        public override Act CreateScript(Creature creature)
        {
            return null;
            //return new GetItemWithTagsAct(creature.AI, Tags);
        }
    }

}