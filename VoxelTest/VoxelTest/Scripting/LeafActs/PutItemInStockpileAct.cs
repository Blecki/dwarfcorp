using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature puts the object currently in its hands into a stockpile.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PutItemInStockpileAct : CreatureAct
    {
        [JsonIgnore]
        public Stockpile Pile { get { return GetPile();  } set{ SetPile(value);} }

        public Stockpile GetPile()
        {
            return Agent.Blackboard.GetData<Stockpile>(StockpileName);
        }

        public void SetPile(Stockpile pile)
        {
            Agent.Blackboard.SetData<Stockpile>(StockpileName, pile);   
        }


        [JsonIgnore]
        public VoxelRef Voxel { get { return GetVoxel(); } set { SetVoxel(value); } }

        public VoxelRef GetVoxel()
        {
            return Agent.Blackboard.GetData<VoxelRef>(VoxelName);
        }

        public void SetVoxel(VoxelRef voxel)
        {
            Agent.Blackboard.SetData(VoxelName, voxel);
        }

        public string StockpileName { get; set; }
        public string VoxelName { get; set; }

        public PutItemInStockpileAct(CreatureAIComponent agent, string stockpileName, string voxelname) :
            base(agent)
        {
            VoxelName = voxelname;
            Name = "Put Item";
            StockpileName = stockpileName;
        }

        public override IEnumerable<Status> Run()
        {
            if(Pile == null && Agent.TargetStockpile != null)
            {
                Pile = Agent.TargetStockpile;
            }
            else if(Pile != null)
            {
                Agent.TargetStockpile = Pile;
            }
            else
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
                yield break;
            }


            LocatableComponent grabbed = Creature.Hands.GetFirstGrab();

            if(grabbed == null)
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
            }
            else if(Pile.AddItem(grabbed, Voxel))
            {
                Creature.Hands.UnGrab(grabbed);

                Matrix m = Matrix.Identity;
                m.Translation = Voxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f);
                grabbed.LocalTransform = m;
                grabbed.HasMoved = true;
                grabbed.DrawBoundingBox = false;


                yield return Status.Success;
            }
            else
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
            }
        }
    }

}