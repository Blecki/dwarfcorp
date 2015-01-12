using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Scripting.TaskManagement.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Farm : Room
    {
        public class FarmTile
        {
            public Voxel Vox = null;
            public Body Plant = null;
            public float Progress = 0.0f;

            public bool IsFree()
            {
                return Plant == null || Plant.IsDead;
            }
        }

        public List<FarmTile> FarmTiles = new List<FarmTile>();

        public virtual Body CreatePlant(Vector3 position)
        {
            return null;
        }

        public override void AddVoxel(Voxel voxel)
        {
            FarmTiles.Add(new FarmTile(){Vox = voxel, Plant = null}); 
            base.AddVoxel(voxel);
        }

        public override void RemoveVoxel(Voxel voxel)
        {
            FarmTile toRemove = FarmTiles.FirstOrDefault(tile => tile.Vox.Equals(voxel));
            if (toRemove != null)
            {
                if(toRemove.Plant != null)
                    toRemove.Plant.Die();
                FarmTiles.Remove(toRemove);
            }
            base.RemoveVoxel(voxel);
        }

        [JsonIgnore]
        public Button FarmButton { get; set; }

        public Farm()
        {

        }

         public Farm(bool designation, IEnumerable<Voxel> designations, RoomData data, ChunkManager chunks) :
            base(designation, designations, data, chunks)
        {

        }

         public Farm(IEnumerable<Voxel> voxels, RoomData data, ChunkManager chunks) :
            base(voxels, data, chunks)
        {

        }

        public void CreatePlant(FarmTile tile)
        {
            tile.Plant = CreatePlant(tile.Vox.Position + new Vector3(0.0f, 1.5f, 0.0f));
            Matrix original = tile.Plant.LocalTransform;
            original.Translation += Vector3.Down;
            tile.Plant.AnimationQueue.Add(new EaseMotion(0.5f, original, tile.Plant.LocalTransform.Translation));
            PlayState.ParticleManager.Trigger("puff", original.Translation, Color.White, 20);
            SoundManager.PlaySound(ContentPaths.Audio.pluck, tile.Vox.Position, true);
            AddBody(tile.Plant);
        }

        public FarmTile GetNearestFreeFarmTile(Vector3 position)
        {
            float closestDist = float.MaxValue;
            FarmTile closest = null;
            foreach (FarmTile tile in FarmTiles)
            {
                if (tile.IsFree())
                {
                    float dist = (tile.Vox.Position - position).LengthSquared();
                    if (dist < closestDist)
                    {
                        closest = tile;
                        closestDist = dist;
                    }
                }
            }

            return closest;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            FarmButton = new Button(PlayState.GUI, PlayState.GUI.RootComponent, "Farm", PlayState.GUI.DefaultFont,
               Button.ButtonMode.ImageButton, new NamedImageFrame(ContentPaths.GUI.icons, 32, 5, 1))
            {
                LocalBounds = new Rectangle(0, 0, 32, 32),
                DrawFrame = true,
                TextColor = Color.White,
                ToolTip = "Click to make selected employees work this " + RoomData.Name
            };
            FarmButton.OnClicked += farmButton_OnClicked;
            GUIObject = new WorldGUIObject(PlayState.ComponentManager.RootComponent, FarmButton)
            {
                IsVisible = true,
                LocalTransform = Matrix.CreateTranslation(GetBoundingBox().Center())
            };
        }

        public override void OnBuilt()
        {
            FarmButton = new Button(PlayState.GUI, PlayState.GUI.RootComponent, "Farm", PlayState.GUI.DefaultFont,
                Button.ButtonMode.ImageButton, new NamedImageFrame(ContentPaths.GUI.icons, 32, 5, 1))
            {
                LocalBounds = new Rectangle(0, 0, 32, 32),
                DrawFrame = true,
                TextColor = Color.White,
                ToolTip = "Click to make selected employees work this " + RoomData.Name
            };
            FarmButton.OnClicked += farmButton_OnClicked;
            GUIObject = new WorldGUIObject(PlayState.ComponentManager.RootComponent, FarmButton)
            {
                IsVisible = true,
                LocalTransform = Matrix.CreateTranslation(GetBoundingBox().Center())
            };
            base.OnBuilt();
        }

        void farmButton_OnClicked()
        {
            List<CreatureAI> minions = PlayState.Master.SelectedMinions.Where(minion => minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Farm)).ToList();
            foreach (CreatureAI creature in minions)
            {
                FarmTask task = new FarmTask(this);

                if (!creature.Tasks.Contains(task))
                    creature.Tasks.Add(task);
            }

            if (minions.Count == 0)
            {
                PlayState.GUI.ToolTipManager.Popup("None of the selected units can farm.");
            }
        }

        public override void Destroy()
        {
            if (GUIObject != null)
            {
                GUIObject.Die();
            }
            base.Destroy();
        }
    }
}
