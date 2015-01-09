using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    /// <summary>
    /// A stockpile is a kind of zone which contains items on top of it.
    /// </summary>
    public class Stockpile : Room
    {
        private static uint maxID = 0;
        public List<Body> Boxes { get; set; }
        public static string StockpileName = "Stockpile";

        public static uint NextID()
        {
            maxID++;
            return maxID;
        }


        public Stockpile(string id, ChunkManager chunk) :
            base(false, new List<Voxel>(), RoomLibrary.GetData(StockpileName), PlayState.ChunkManager)
        {
            Boxes = new List<Body>();
            ReplacementType = VoxelLibrary.GetVoxelType("Stockpile");
        }

        public void KillBox(Body component)
        {
            EaseMotion deathMotion = new EaseMotion(0.8f, component.LocalTransform, component.LocalTransform.Translation + new Vector3(0, -1, 0));
            component.AnimationQueue.Add(deathMotion);
            deathMotion.OnComplete += component.Die;
            SoundManager.PlaySound(ContentPaths.Audio.whoosh, component.LocalTransform.Translation);
            PlayState.ParticleManager.Trigger("puff", component.LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 90);
        }

        public void CreateBox(Vector3 pos)
        {
            Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);
            Vector3 endPos = pos + new Vector3(0.0f, 0.9f, 0.0f);

            Body crate = EntityFactory.CreateEntity<Body>("Crate", startPos);
            crate.AnimationQueue.Add(new EaseMotion(0.8f, crate.LocalTransform, endPos));
            Boxes.Add(crate);
            SoundManager.PlaySound(ContentPaths.Audio.whoosh, startPos);
            PlayState.ParticleManager.Trigger("puff", pos + new Vector3(0.5f, 1.5f, 0.5f), Color.White, 90);
        }

        public void HandleBoxes()
        {
            if(Voxels.Count == 0)
            {
                foreach(Body component in Boxes)
                {
                    KillBox(component);
                }
                Boxes.Clear();
            }

            int numBoxes = Math.Min(Math.Max(Resources.CurrentResourceCount / ResourcesPerVoxel, 1), Voxels.Count);

            if (Boxes.Count > numBoxes)
            {
                for (int i = Boxes.Count - 1; i >= numBoxes; i--)
                {
                    KillBox(Boxes[i]);
                    Boxes.RemoveAt(i);
                }
            }
            else if (Boxes.Count < numBoxes)
            {
                for (int i = Boxes.Count; i < numBoxes; i++)
                {
                    CreateBox(Voxels[i].Position);
                }
            }
        }

       

        public override bool AddItem(Body component)
        {
            bool worked =  base.AddItem(component);
            HandleBoxes();

            TossMotion toss = new TossMotion(1.0f, 2.5f, component.LocalTransform, Boxes[Boxes.Count - 1].LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f));
            component.AnimationQueue.Add(toss);
            toss.OnComplete += component.Die;

            return worked;
        }


        public override void Destroy()
        {
            BoundingBox box = GetBoundingBox();
            foreach (ResourceAmount resource in Resources)
            {
                for (int i = 0; i < resource.NumResources; i++)
                {
                    Physics body = EntityFactory.CreateEntity<Physics>(resource.ResourceType.Type + " Resource",
                        Vector3.Up + MathFunctions.RandVector3Box(box)) as Physics;

                    if (body != null)
                    {
                        body.Velocity = MathFunctions.RandVector3Cube();
                    }
                }
            }
            base.Destroy();
        }

        public override void RecalculateMaxResources()
        {

            HandleBoxes();
            base.RecalculateMaxResources();
        }

        public static RoomData InitializeData()
        {
           List<RoomTemplate> stockpileTemplates = new List<RoomTemplate>();
           Dictionary<ResourceLibrary.ResourceType, ResourceAmount> stockpileResources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();

            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(StockpileName, 0, "Stockpile", stockpileResources, stockpileTemplates, new ImageFrame(roomIcons, 16, 0, 0))
            {
                Description = "Dwarves can stock resources here",
            };
        }
    }

}