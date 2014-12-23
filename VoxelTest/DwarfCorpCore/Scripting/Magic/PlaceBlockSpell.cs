using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class PlaceBlockSpell : Spell
    {
        public string VoxelType { get; set; }
        public bool Transmute { get; set; }

        public PlaceBlockSpell(string voxelType, bool transmute)
        {
            Texture2D icons = TextureManager.GetTexture(ContentPaths.GUI.icons);
            Image = new ImageFrame(icons, 32, 1, 2);
            VoxelType = voxelType;
            Transmute = transmute;
            if (!transmute)
            {
                Name = VoxelType + " Wall";
                Description = "Create a wall of " + VoxelType;
                Hint = "Click and drag to place " + VoxelType;
                Recharges = false;
                RechargeTimer = new Timer(5.0f, false);
                ManaCost = 150;
                Mode = SpellMode.SelectEmptyVoxels;
            }
            else
            {
                Name = "Transmute " + VoxelType;
                Description = "Transmute any block into " + VoxelType;
                Hint = "Click and drag to transmute.";
                Recharges = false;
                RechargeTimer = new Timer(5.0f, false);
                ManaCost = 250;
                Mode = SpellMode.SelectEmptyVoxels;
            }

        }

        public override void OnVoxelsSelected(List<Voxel> voxels)
        {
            HashSet<Point3> chunksToRebuild = new HashSet<Point3>();
            bool placed = false;
            foreach (Voxel selected in voxels)
            {

                if (selected != null && ((!Transmute && selected.IsEmpty) || Transmute && !selected.IsEmpty))
                {
                    PlayState.ParticleManager.Trigger("star_particle", selected.Position + Vector3.One * 0.5f, Color.White, 4);
                    VoxelLibrary.PlaceType(VoxelLibrary.GetVoxelType(VoxelType), selected);

                    if (VoxelType == "Magic")
                    {
                        new VoxelListener(PlayState.ComponentManager, PlayState.ComponentManager.RootComponent, PlayState.ChunkManager, selected)
                        {
                            DestroyOnTimer = true,
                            DestroyTimer = new Timer(5.0f + MathFunctions.Rand(-0.5f, 0.5f), true)
                        };
                    }
                    placed = true;
                    chunksToRebuild.Add(selected.ChunkID);
                }
            }

            foreach (Point3 point in chunksToRebuild)
            {
                VoxelChunk chunk = PlayState.ChunkManager.ChunkData.ChunkMap[point];
                chunk.ShouldRebuild = true;
                chunk.NotifyTotalRebuild(true);
            }

            if (placed)
            {
                SoundManager.PlaySound(ContentPaths.Audio.tinkle, PlayState.CursorLightPos, true, 1.0f);
            }

            RechargeTimer.Reset(RechargeTimer.TargetTimeSeconds);
            base.OnVoxelsSelected(voxels);
        }
    }
}
