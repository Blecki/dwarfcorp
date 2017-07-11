using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public struct NewVoxelHandle
    {
        [JsonIgnore]
        private WorldManager World;
        public GlobalVoxelCoordinate Coordinate;

        public NewVoxelHandle(WorldManager World, GlobalVoxelCoordinate Coordinate)
        {
            this.World = World;
            this.Coordinate = Coordinate;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            World = ((WorldManager)context.Context);
        }

        // Cache chunk, local coords, make immutable
    }
}
