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
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Scripting.TaskManagement.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Using this tool, the player can specify regions of voxels to be
    /// turned into rooms.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class FarmTool : PlayerTool
    {
        public FarmingPanel FarmPanel { get; set; }
        public BuildMenu.BuildType BuildType { get; set; }
        public string PlantType { get; set; }
        public List<ResourceAmount> RequiredResources { get; set; } 
        public enum FarmMode
        {
            Tilling,
            Planting,
            Harvesting
        }

        public FarmMode Mode { get; set; }

        public class FarmTile
        {
            public Voxel Vox = null;
            public Body Plant = null;
            public float Progress = 0.0f;
            public CreatureAI Farmer = null;

            public bool IsTilled()
            {
                return (Vox != null) && Vox.TypeName == "TilledSoil";
            }

            public bool IsFree()
            {
                return (Plant == null || Plant.IsDead) && Farmer == null;
            }

            public bool PlantExists()
            {
                return !(Plant == null || Plant.IsDead);
            }

            public void CreatePlant(string plantToCreate)
            {
                Plant = EntityFactory.CreateEntity<Body>(ResourceLibrary.Resources[plantToCreate].PlantToGenerate, Vox.Position + Vector3.Up * 1.5f);
                Matrix original = Plant.LocalTransform;
                original.Translation += Vector3.Down;
                Plant.AnimationQueue.Add(new EaseMotion(0.5f, original, Plant.LocalTransform.Translation));
                PlayState.ParticleManager.Trigger("puff", original.Translation, Color.White, 20);
                SoundManager.PlaySound(ContentPaths.Audio.pluck, Vox.Position, true);
            }
        }

        public List<FarmTile> FarmTiles = new List<FarmTile>();

        public bool HasTile(Voxel vox)
        {
            return FarmTiles.Any(f => f.Vox.Equals(vox));
        }


        public bool HasPlant(Voxel vox)
        {
            return HasTile(vox) && FarmTiles.Any(f => f.Vox.Equals(vox) && f.PlantExists());
        }

        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
            List<CreatureAI> minions = PlayState.Master.SelectedMinions.Where(minion => minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Farm)).ToList();
            List<Task> goals = new List<Task>();
            switch (Mode)
            {
                case FarmMode.Tilling:
                    foreach (Voxel voxel in voxels)
                    {
                        if (button == InputManager.MouseButton.Left)
                        {
                            if (!voxel.Type.IsSoil)
                            {
                                PlayState.GUI.ToolTipManager.Popup("Can only till soil!");
                                continue;
                            }
                            if (voxel.TypeName == "TilledSoil")
                            {
                                PlayState.GUI.ToolTipManager.Popup("Soil already tilled!");
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
                            if (HasTile(voxel) && !HasPlant(voxel))
                            {
                                FarmTiles.RemoveAll(tile => tile.Vox.Equals(voxel));
                            }
                        }
                    }
                    TaskManager.AssignTasksGreedy(goals, minions, 1);
                    break;
                case FarmMode.Planting:
                    int currentAmount =
                        Player.Faction.ListResources()
                        .Sum(resource => resource.Key == PlantType && resource.Value.NumResources > 0 ? resource.Value.NumResources : 0);
                    foreach (Voxel voxel in voxels)
                    {

                        if (currentAmount == 0)
                        {
                            PlayState.GUI.ToolTipManager.Popup("Not enough " + PlantType + " in stocks!");
                            break;
                        }
                        if (voxel.TypeName != "TilledSoil")
                        {
                            PlayState.GUI.ToolTipManager.Popup("Can only plant on tilled soil!");
                            continue;
                        }

                        if (ResourceLibrary.Resources[PlantType].Tags.Contains(Resource.ResourceTags.AboveGroundPlant))
                        {
                            if (voxel.SunColor == 0)
                            {
                                PlayState.GUI.ToolTipManager.Popup("Can only plant " + PlantType + " above ground.");
                                continue;
                            }
                        }
                        else if (
                            ResourceLibrary.Resources[PlantType].Tags.Contains(
                                Resource.ResourceTags.BelowGroundPlant))
                        {
                            if (voxel.SunColor > 0)
                            {
                                PlayState.GUI.ToolTipManager.Popup("Can only plant " + PlantType + " below ground.");
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
                            PlayState.GUI.ToolTipManager.Popup("Something is already planted here!");
                            continue;
                        }
                    }
                    TaskManager.AssignTasksGreedy(goals, minions, 1);
                    break;
            }
        }


        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            if (Mode == FarmMode.Harvesting)
            {
                List<Body> treesPickedByMouse = ComponentManager.FilterComponentsWithTag("Vegetation", bodies);

                foreach (Body tree in treesPickedByMouse)
                {
                    if (!tree.IsVisible || tree.IsAboveCullPlane) continue;

                    Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                    if (button == InputManager.MouseButton.Left)
                    {
                        if (!Player.Faction.ChopDesignations.Contains(tree))
                        {
                            Player.Faction.ChopDesignations.Add(tree);

                            foreach (CreatureAI creature in Player.Faction.SelectedMinions)
                            {
                                creature.Tasks.Add(new KillEntityTask(tree, KillEntityTask.KillType.Chop) { Priority = Task.PriorityType.Low });
                            }
                        }
                    }
                    else if (button == InputManager.MouseButton.Right)
                    {
                        if (Player.Faction.ChopDesignations.Contains(tree))
                        {
                            Player.Faction.ChopDesignations.Remove(tree);
                        }
                    }
                }
            }
        }


        public override void OnBegin()
        {
            if (FarmPanel != null)
            {
                FarmPanel.Destroy();
            }
            int w = 600;
            int h = 350;
            FarmPanel = new FarmingPanel(PlayState.GUI, PlayState.GUI.RootComponent)
            {
                LocalBounds = new Rectangle(PlayState.Game.GraphicsDevice.Viewport.Width / 2 - w / 2, PlayState.Game.GraphicsDevice.Viewport.Height / 2 - h / 2, w, h),
                IsVisible = true,
                DrawOrder = 2
            };
            FarmPanel.OnHarvest += FarmPanel_OnHarvest;
            FarmPanel.OnPlant += FarmPanel_OnPlant;
            FarmPanel.OnTill += FarmPanel_OnTill;
            FarmPanel.TweenIn(Drawer2D.Alignment.Right, 0.25f);
        }

        void FarmPanel_OnTill()
        {
            PlayState.GUI.ToolTipManager.Popup("Click and drag to till soil.");
            Mode = FarmMode.Tilling;
        }

        void FarmPanel_OnPlant(string plantType, string resource)
        {
            PlayState.GUI.ToolTipManager.Popup("Click and drag to plant " + plantType + ".");
            Mode = FarmMode.Planting;
            PlantType = plantType;
            RequiredResources = new List<ResourceAmount>() {new ResourceAmount(resource)};
        }

        void FarmPanel_OnHarvest()
        {
            PlayState.GUI.ToolTipManager.Popup("Click and drag to harvest.");
            Mode = FarmMode.Harvesting;
        }

        public override void OnEnd()
        {
            FarmPanel.TweenOut(Drawer2D.Alignment.Right, 0.25f);
        }


        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = false;
                Player.BodySelector.Enabled = false;
                return;
            }

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
            }
            PlayState.GUI.IsMouseVisible = true;

            PlayState.GUI.MouseMode = PlayState.GUI.IsMouseOver() ? GUISkin.MousePointer.Pointer : GUISkin.MousePointer.Farm;
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

                    foreach (BoundingBox box in FarmTiles.Where(tile => !tile.IsTilled()).Select(tile => tile.Vox.GetBoundingBox()))
                    {
                        Drawer3D.DrawBox(box, drawColor, 0.05f*alpha + 0.05f, true);
                    }

                    break;
                }
                case FarmMode.Planting:
                {
                    Color drawColor = Color.LimeGreen;

                    float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 2.0f));
                    drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
                    drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
                    drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));


                    foreach (BoundingBox box in FarmTiles.Where(tile => tile.IsTilled() && !tile.PlantExists() && tile.Farmer == null).Select(tile => tile.Vox.GetBoundingBox()))
                    {
                        Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, true);
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
            }
        }

    }
}
