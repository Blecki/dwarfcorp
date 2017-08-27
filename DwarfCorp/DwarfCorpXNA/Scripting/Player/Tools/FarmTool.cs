// BuildTool.cs
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
using System.Drawing;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{
    /// <summary>
    /// Using this tool, the player can specify regions of voxels to be
    /// turned into rooms.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class FarmTool : PlayerTool
    {
        public BuildTypes BuildType { get; set; }
        public string PlantType { get; set; }
        public List<ResourceAmount> RequiredResources { get; set; } 
        public enum FarmMode
        {
            Tilling,
            Planting,
            Harvesting,
            WranglingAnimals
        }

        public FarmMode Mode { get; set; }

        public class FarmTile
        {
            public VoxelHandle Vox = VoxelHandle.InvalidHandle;
            public Plant Plant = null;
            public float Progress = 0.0f;
            public CreatureAI Farmer = null;
            public bool IsCanceled = false;

            public bool IsTilled()
            {
                return (Vox.IsValid) && Vox.Type.Name == "TilledSoil";
            }

            public bool IsFree()
            {
                return (Plant == null || Plant.IsDead) && Farmer == null;
            }

            public bool PlantExists()
            {
                return !(Plant == null || Plant.IsDead);
            }

            public void CreatePlant(string plantToCreate, WorldManager world)
            {
                Plant = EntityFactory.CreateEntity<Plant>(ResourceLibrary.Resources[plantToCreate].PlantToGenerate, Vox.WorldPosition + Vector3.Up * 1.5f);
                Seedling seed = Plant.BecomeSeedling();

                Matrix original = Plant.LocalTransform;
                original.Translation += Vector3.Down;
                seed.AnimationQueue.Add(new EaseMotion(0.5f, original, Plant.LocalTransform.Translation));
                 
                world.ParticleManager.Trigger("puff", original.Translation, Color.White, 20);
                
                SoundManager.PlaySound(ContentPaths.Audio.pluck, Vox.WorldPosition, true);
                
            }
        }

        public List<FarmTile> FarmTiles = new List<FarmTile>();

        public bool HasTile(VoxelHandle vox)
        {
            return FarmTiles.Any(f => f.Vox == vox);
        }


        public bool HasPlant(VoxelHandle vox)
        {
            return HasTile(vox) && FarmTiles.Any(f => f.Vox.Equals(vox) && f.PlantExists());
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            List<CreatureAI> minions = Player.World.Master.SelectedMinions.Where(minion => minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Farm)).ToList();
            List<Task> goals = new List<Task>();
            switch (Mode)
            {
                case FarmMode.Tilling:
                    foreach (var voxel in voxels)
                    {
                        if (button == InputManager.MouseButton.Left)
                        {
                            if (!voxel.Type.IsSoil)
                            {
                                Player.World.ShowToolPopup("Can only till soil!");
                                continue;
                            }
                            if (voxel.Type.Name == "TilledSoil")
                            {
                                Player.World.ShowToolPopup("Soil already tilled!");
                                continue;
                            }
                            if (!HasTile(voxel))
                            {
                                FarmTile tile = new FarmTile() {Vox = voxel};
                                goals.Add(new FarmTask(tile) {Mode = FarmAct.FarmMode.Till, Plant = PlantType});
                                FarmTiles.Add(tile);
                            }
                            else
                            {
                                goals.Add(new FarmTask(FarmTiles.Find(tile => tile.Vox.Equals(voxel)))
                                {
                                    Mode = FarmAct.FarmMode.Till,
                                    Plant = PlantType
                                });
                            }
                        }
                        else
                        {
                            if (!HasTile(voxel) || HasPlant(voxel)) continue;
                            Drawer3D.UnHighlightVoxel(voxel);
                            foreach (FarmTile tile in FarmTiles)
                            {
                                if (tile.Vox.Equals(voxel))
                                    tile.IsCanceled = true;
                            }
                            FarmTiles.RemoveAll(tile => tile.Vox.Equals(voxel));
                        }
                    }
                    TaskManager.AssignTasksGreedy(goals, minions, 1);

                    foreach (CreatureAI creature in minions)
                    {
                        creature.Creature.NoiseMaker.MakeNoise("Ok", creature.Position);
                    }

                    break;
                case FarmMode.Planting:
                    int currentAmount =
                        Player.Faction.ListResources()
                        .Sum(resource => resource.Key == PlantType && resource.Value.NumResources > 0 ? resource.Value.NumResources : 0);
                    foreach (var voxel in voxels)
                    {

                        if (currentAmount == 0)
                        {
                            Player.World.ShowToolPopup("Not enough " + PlantType + " in stocks!");
                            break;
                        }
                        if (voxel.Type.Name != "TilledSoil")
                        {
                            Player.World.ShowToolPopup("Can only plant on tilled soil!");
                            continue;
                        }

                        if (ResourceLibrary.Resources[PlantType].Tags.Contains(Resource.ResourceTags.AboveGroundPlant))
                        {
                            if (voxel.SunColor == 0)
                            {
                                Player.World.ShowToolPopup("Can only plant " + PlantType + " above ground.");
                                continue;
                            }
                        }
                        else if (
                            ResourceLibrary.Resources[PlantType].Tags.Contains(
                                Resource.ResourceTags.BelowGroundPlant))
                        {
                            if (voxel.SunColor > 0)
                            {
                                Player.World.ShowToolPopup("Can only plant " + PlantType + " below ground.");
                                continue;
                            }
                        }

                        if (!HasPlant(voxel))
                        {
                            FarmTile tile = new FarmTile() { Vox = voxel };
                            goals.Add(new FarmTask(tile) {  Mode = FarmAct.FarmMode.Plant, Plant = PlantType, RequiredResources = RequiredResources});
                            FarmTiles.Add(tile);
                            currentAmount--;
                        }
                        else
                        {
                            Player.World.ShowToolPopup("Something is already planted here!");
                            continue;
                        }
                    }
                    TaskManager.AssignTasksGreedy(goals, minions, 1);
                    OnConfirm(minions);
                    break;
            }
        }


        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            if (Mode == FarmMode.Harvesting)
            {
                List<Task> tasks = new List<Task>();
                foreach (Body tree in bodies.Where(c => c.Tags.Contains("Vegetation")))
                {
                    if (!tree.IsVisible || tree.IsAboveCullPlane(Player.World.ChunkManager)) continue;

                    Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                    if (button == InputManager.MouseButton.Left)
                    {
                        if (!Player.Faction.ChopDesignations.Contains(tree))
                        {
                            Player.Faction.ChopDesignations.Add(tree);
                            tasks.Add(new KillEntityTask(tree, KillEntityTask.KillType.Chop) { Priority = Task.PriorityType.Low });
                            this.Player.World.ShowToolPopup("Will harvest this " + tree.Name);
                        }
                    }
                    else if (button == InputManager.MouseButton.Right)
                    {
                        if (Player.Faction.ChopDesignations.Contains(tree))
                        {
                            Player.Faction.ChopDesignations.Remove(tree);
                            this.Player.World.ShowToolPopup("Harvest cancelled " + tree.Name);
                        }
                    }
                }
                if (tasks.Count > 0 && Player.SelectedMinions.Count > 0)
                {
                    TaskManager.AssignTasks(tasks, Player.SelectedMinions);
                    OnConfirm(Player.SelectedMinions);
                }
            }
            else if (Mode == FarmMode.WranglingAnimals)
            {
                List<Task> tasks = new List<Task>();
                foreach (Body animal in bodies.Where(c => c.Tags.Contains("DomesticAnimal")))
                {
                    Drawer3D.DrawBox(animal.BoundingBox, Color.Tomato, 0.1f, false);
                    if (button == InputManager.MouseButton.Left)
                    {
                        if (!Player.Faction.WrangleDesignations.Contains(animal))
                        {
                            Player.Faction.WrangleDesignations.Add(animal);
                            tasks.Add(new WrangleAnimalTask(animal.GetRoot().GetComponent<Creature>()));
                            this.Player.World.ShowToolPopup("Will wrangle this " + animal.Name);
                        }
                    }
                    else if (button == InputManager.MouseButton.Right)
                    {
                        if (Player.Faction.WrangleDesignations.Contains(animal))
                        {
                            Player.Faction.WrangleDesignations.Remove(animal);
                            this.Player.World.ShowToolPopup("Wrangle cancelled " + animal.Name);
                        }
                    }
                }
                if (tasks.Count > 0 && Player.SelectedMinions.Count > 0)
                {
                    TaskManager.AssignTasks(tasks, Player.SelectedMinions);
                    OnConfirm(Player.SelectedMinions);
                }
            }
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }


        public override void OnBegin()
        {
            /*
            if (FarmPanel != null)
            {
                FarmPanel.Destroy();
            }
            int w = 600;
            int h = 350;
            FarmPanel = new FarmingPanel(Player.World.GUI, Player.World.GUI.RootComponent, Player.World)
            {
                LocalBounds = new Rectangle(PlayState.Game.GraphicsDevice.Viewport.Width / 2 - w / 2, PlayState.Game.GraphicsDevice.Viewport.Height / 2 - h / 2, w, h),
                IsVisible = true,
                DrawOrder = 2
            };
            FarmPanel.OnHarvest += FarmPanel_OnHarvest;
            FarmPanel.OnPlant += FarmPanel_OnPlant;
            FarmPanel.OnTill += FarmPanel_OnTill;
            FarmPanel.TweenIn(Drawer2D.Alignment.Right, 0.25f);
             */
        }

        void FarmPanel_OnTill()
        {
            Player.World.ShowToolPopup("Click and drag to till soil.");
            Mode = FarmMode.Tilling;
        }

        void FarmPanel_OnPlant(string plantType, string resource)
        {
            Player.World.ShowToolPopup("Click and drag to plant " + plantType + ".");
            Mode = FarmMode.Planting;
            PlantType = plantType;
            RequiredResources = new List<ResourceAmount>() {new ResourceAmount(resource)};
        }

        void FarmPanel_OnHarvest()
        {
            Player.World.ShowToolPopup("Click and drag to harvest.");
            Mode = FarmMode.Harvesting;
        }

        public override void OnEnd()
        {
            //FarmPanel.TweenOut(Drawer2D.Alignment.Right, 0.25f);
            foreach (FarmTile tile in FarmTiles)
            {
                Drawer3D.UnHighlightVoxel(tile.Vox);
            }
        }


        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                Player.BodySelector.Enabled = false;
                return;
            }

            Player.BodySelector.AllowRightClickSelection = true;

            switch (Mode)
            {
               case FarmMode.Tilling:
                    Player.VoxSelector.Enabled = true;
                    Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                    Player.BodySelector.Enabled = false;
                    break;
                case FarmMode.Planting:
                    Player.VoxSelector.Enabled = true;
                    Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                    Player.BodySelector.Enabled = false;
                    break;
                case FarmMode.Harvesting:
                    Player.VoxSelector.Enabled = false;
                    Player.BodySelector.Enabled = true;
                    break;
                case FarmMode.WranglingAnimals:
                    Player.VoxSelector.Enabled = false;
                    Player.BodySelector.Enabled = true;
                    break;
            }

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 12));
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            switch (Mode)
            {
                case FarmMode.Tilling:
                {
                    Color drawColor = Color.PaleGoldenrod;

                    float alpha = (float) Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds*2.0f));
                    drawColor.R = (byte) (Math.Min(drawColor.R*alpha + 50, 255));
                    drawColor.G = (byte) (Math.Min(drawColor.G*alpha + 50, 255));
                    drawColor.B = (byte) (Math.Min(drawColor.B*alpha + 50, 255));

                    foreach (var tile in FarmTiles)
                    {
                        if (!tile.IsTilled())
                        {
                            Drawer3D.HighlightVoxel(tile.Vox, Color.LimeGreen);
                        }
                        else
                        {
                            Drawer3D.UnHighlightVoxel(tile.Vox);
                        }
                    }
                    break;
                }
                case FarmMode.Planting:
                {
                    foreach (var tile in FarmTiles)
                    {
                        if (tile.IsTilled() && !tile.PlantExists() && tile.Farmer == null)
                        {
                            Drawer3D.HighlightVoxel(tile.Vox, Color.LimeGreen);
                        }
                        else
                        {
                            Drawer3D.UnHighlightVoxel(tile.Vox);
                        }
                    }

                    break;
                }

                case FarmMode.Harvesting:
                {
                    Color drawColor = Color.LimeGreen;

                    float alpha = (float) Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds*2.0f));
                    drawColor.R = (byte) (Math.Min(drawColor.R*alpha + 50, 255));
                    drawColor.G = (byte) (Math.Min(drawColor.G*alpha + 50, 255));
                    drawColor.B = (byte) (Math.Min(drawColor.B*alpha + 50, 255));

                    foreach (BoundingBox box in Player.Faction.ChopDesignations.Select(d => d.GetBoundingBox()))
                    {
                        Drawer3D.DrawBox(box, drawColor, 0.05f*alpha + 0.05f, true);
                    }
                    break;
                }

                case FarmMode.WranglingAnimals:
                {
                    Color drawColor = Color.Tomato;
                    float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 2.0f));
                    drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
                    drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
                    drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));
                    foreach (BoundingBox box in Player.Faction.WrangleDesignations.Select(d => d.GetBoundingBox()))
                    {
                        Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, true);
                    }
                    break;
                }
            }
        }

        public KillEntityTask AutoFarm()
        {
            return (from tile in FarmTiles where tile.PlantExists() && tile.Plant.IsGrown && !tile.IsCanceled select new KillEntityTask(tile.Plant, KillEntityTask.KillType.Chop)).FirstOrDefault();
        }
    }

    public class WrangleAnimalTask : Task
    {
        public Creature Animal { get; set; }
        public AnimalPen LastPen { get; set; }

        public WrangleAnimalTask()
        {
            
        }
        public WrangleAnimalTask(Creature animal)
        {
            Animal = animal;
            Name = "Wrangle animal" + animal.GlobalID;
            AutoRetry = true;
        }

        public IEnumerable<Act.Status> PenAnimal(CreatureAI agent, CreatureAI creature, AnimalPen animalPen)
        {
            foreach (var status in animalPen.AddAnimal(Animal.Physics, agent.Faction))
            {
                if (status == Act.Status.Fail)
                {
                    creature.PositionConstraint = null;
                    yield return Act.Status.Fail;
                    yield break;
                }
            }
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> ReleaseAnimal(CreatureAI animal)
        {
            animal.PositionConstraint = null;
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> WrangleAnimal(CreatureAI agent, CreatureAI creature)
        {
            creature.PositionConstraint = new BoundingBox(agent.Position - new Vector3(1.0f, 0.5f, 1.0f), 
                agent.Position + new Vector3(1.0f, 0.5f, 1.0f));
            Drawer3D.DrawLine(creature.Position, agent.Position, Color.Black, 0.05f);
            yield return Act.Status.Success;
        }

        public AnimalPen GetClosestPen(Creature agent)
        {
            if (LastPen != null && (LastPen.Species == "" || LastPen.Species == Animal.Species) && agent.Faction.GetRooms().Contains(LastPen))
            {
                return LastPen;
            }

            var pens = agent.Faction.GetRooms().Where(room => room is AnimalPen).Cast<AnimalPen>().Where(pen => pen.Species == "" || pen.Species == Animal.Species);
            AnimalPen closestPen = null;
            float closestDist = float.MaxValue;

            foreach (var pen in pens)
            {
                var dist = (pen.GetBoundingBox().Center() - agent.Physics.Position).LengthSquared();
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPen = pen;
                }
            }
            if (closestPen == null)
            {
                agent.World.MakeAnnouncement("Can't wrangle " + Animal.Species + "s. Need more animal pens.");
            }
            LastPen = closestPen;
            return closestPen;
        }

        public override Act CreateScript(Creature agent)
        {
            var closestPen = GetClosestPen(agent);
            if (closestPen == null)
            {
                return null;
            }

            closestPen.Species = Animal.Species;

            return new Select(new Sequence(new Domain(() => IsFeasible(agent), new GoToEntityAct(Animal.Physics, agent.AI)),
                new Domain(() => IsFeasible(agent), new Parallel(new Repeat(new Wrap(() => WrangleAnimal(agent.AI, Animal.AI)), -1, false),
                new GoToZoneAct(agent.AI, closestPen)) { ReturnOnAllSucces = false}),
                new Domain(() => IsFeasible(agent), new Wrap(() => PenAnimal(agent.AI, Animal.AI, closestPen)))), 
                new Wrap(() => ReleaseAnimal(Animal.AI)));
        }

        public override Task Clone()
        {
            return new WrangleAnimalTask(Animal);
        }

        public override bool IsFeasible(Creature agent)
        {
            return agent.Faction.WrangleDesignations.Contains(Animal.GetRoot().GetComponent<Physics>()) && Animal != null && !Animal.IsDead &&
                GetClosestPen(agent) != null;
        }


        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (agent.AI.Position - Animal.Physics.Position).LengthSquared();
        }
    }
}
