using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.IO;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<string, VoxelModel> VoxelModels = new Dictionary<string, VoxelModel>();

        private static bool VoxelModelsInitialized = false;

        private static void InitializeVoxelModels()
        {
            if (VoxelModelsInitialized) return;
            VoxelModelsInitialized = true;

            foreach (var voxFile in AssetManager.EnumerateAllFiles("World/VoxelModels/"))
            {
                try
                {

                    var stream = new FileStream(voxFile, FileMode.Open);
                    var model = new VoxelModel();
                    var modelReader = new CsharpVoxReader.VoxReader(stream, model);
                    modelReader.ReadFromStream();

                    var name = System.IO.Path.GetFileNameWithoutExtension(voxFile);
                    VoxelModels.Add(name, model);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error loading voxel model " + voxFile + ": " + e.Message);
                }
            }

            Console.WriteLine("Loaded Voxel Model Library.");
        }

        public static MaybeNull<VoxelModel> GetVoxelModel(String Name)
        {
            InitializeVoxelModels();
            return VoxelModels[Name];
        }

        public static IEnumerable<VoxelModel> EnumerateVoxelModels()
        {
            InitializeVoxelModels();
            return VoxelModels.Values;
        }
    }
}