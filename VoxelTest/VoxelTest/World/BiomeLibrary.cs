using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A static collection of biome types.
    /// </summary>
    public class BiomeLibrary
    {
        public static Dictionary<Overworld.Biome, BiomeData> Biomes = new Dictionary<Overworld.Biome, BiomeData>();


        public BiomeLibrary()
        {
            InitializeStatics();
        }


        public static void InitializeStatics()
        {
            VegetationData grasslandPines = new VegetationData("pine", 0.001f, 0.5f, 0.5f, 1.7f);
            VegetationData grasslandBushes = new VegetationData("berrybush", 0.005f, 1.0f, 0.1f, 0.6f);
            DetailMoteData greenGrass = new DetailMoteData("grass", ContentPaths.Entities.Plants.grass, 0.1f, 0.6f, 1.0f);
            DetailMoteData flowers = new DetailMoteData("flower", ContentPaths.Entities.Plants.flower, 0.3f, 0.9f, 0.8f);
            FaunaData grasslandBirds = new FaunaData("Bird", 0.0001f);
            BiomeData grassland = new BiomeData(Overworld.Biome.Grassland)
            {
                GrassVoxel = "Grass",
                SoilVoxel = "Dirt",
                SubsurfVoxel = "Stone",
                ShoreVoxel = "Sand"
            };
            grassland.Vegetation.Add(grasslandPines);
            grassland.Vegetation.Add(grasslandBushes);
            grassland.Motes.Add(greenGrass);
            grassland.Motes.Add(flowers);
            grassland.Fauna.Add(grasslandBirds);
            Biomes[Overworld.Biome.Grassland] = grassland;

            VegetationData forestPines = new VegetationData("pine", 0.008f, 1.0f, 0.5f, 1.7f);
            VegetationData forestBushes = new VegetationData("berrybush", 0.004f, 1.0f, 0.1f, 0.6f);
            DetailMoteData forestGrass = new DetailMoteData("grass", ContentPaths.Entities.Plants.grass, 0.1f, 0.6f, 1.0f);
            DetailMoteData forestMushrooms = new DetailMoteData("mushroom",  ContentPaths.Entities.Plants.mushroom, 0.8f, 0.85f, 1.0f);
            FaunaData forestBirds = new FaunaData("Bird", 0.0005f);
            FaunaData forestDeer = new FaunaData("Deer", 0.0005f);
            BiomeData forest = new BiomeData(Overworld.Biome.Forest)
            {
                GrassVoxel = "Grass",
                SoilVoxel = "Dirt",
                SubsurfVoxel = "Stone",
                ShoreVoxel = "Sand"
            };
            forest.Vegetation.Add(forestPines);
            forest.Vegetation.Add(forestBushes);
            forest.Motes.Add(forestGrass);
            forest.Motes.Add(forestMushrooms);
            forest.Fauna.Add(forestBirds);
            forest.Fauna.Add(forestDeer);
            Biomes[Overworld.Biome.Forest] = forest;

            VegetationData snowPines = new VegetationData("snowpine", 0.005f, 1.0f, 0.5f, 1.7f);
            DetailMoteData blueGrass = new DetailMoteData("frostgrass", ContentPaths.Entities.Plants.frostgrass, 0.1f, 0.6f, 1.0f);
            FaunaData snowBirds = new FaunaData("Bird", 0.0001f);
            BiomeData taiga = new BiomeData(Overworld.Biome.ColdForest)
            {
                GrassVoxel = "Frost",
                SoilVoxel = "Dirt",
                SubsurfVoxel = "Stone",
                ShoreVoxel = "Sand"
            };
            taiga.Vegetation.Add(snowPines);
            taiga.Motes.Add(blueGrass);
            taiga.Fauna.Add(snowBirds);
            Biomes[Overworld.Biome.ColdForest] = taiga;

            BiomeData tundra = new BiomeData(Overworld.Biome.Tundra)
            {
                GrassVoxel = "Frost",
                SoilVoxel = "Dirt",
                SubsurfVoxel = "Stone",
                ShoreVoxel = "Sand"
            };
            tundra.Motes.Add(blueGrass);
            Biomes[Overworld.Biome.Tundra] = tundra;

            VegetationData desertPalms = new VegetationData("palm", 0.0005f, 0.5f, 0.5f, 1.7f);
            DetailMoteData brownGrass = new DetailMoteData("gnarled", ContentPaths.Entities.Plants.gnarled, 0.1f, 0.6f, 1.0f);
            DetailMoteData dead = new DetailMoteData("deadbush", ContentPaths.Entities.Plants.gnarled, 0.5f, 0.9f, 1.0f);
            BiomeData desert = new BiomeData(Overworld.Biome.Desert)
            {
                GrassVoxel = "Sand",
                SoilVoxel = "Sand",
                ShoreVoxel = "Sand",
                SubsurfVoxel = "Stone"
            };
            desert.Motes.Add(brownGrass);
            desert.Motes.Add(dead);
            desert.Vegetation.Add(desertPalms);
            Biomes[Overworld.Biome.Desert] = desert;


            VegetationData junglePines = new VegetationData("palm", 0.008f, 2.0f, 1.0f, 1.7f);
            VegetationData jungleBushes = new VegetationData("berrybush", 0.005f, 1.0f, 0.1f, 0.6f);
            DetailMoteData jungleGrass = new DetailMoteData("vine", ContentPaths.Entities.Plants.vine, 0.1f, 0.6f, 1.0f);
            FaunaData junglebirds = new FaunaData("Bird", 0.001f);
            BiomeData jungle = new BiomeData(Overworld.Biome.Jungle)
            {
                GrassVoxel = "Grass",
                SoilVoxel = "Dirt",
                SubsurfVoxel = "Stone",
                ShoreVoxel = "Sand"
            };
            jungle.Vegetation.Add(junglePines);
            jungle.Vegetation.Add(jungleBushes);
            jungle.Motes.Add(jungleGrass);
            jungle.Fauna.Add(junglebirds);
            Biomes[Overworld.Biome.Jungle] = jungle;


            BiomeData vocano = new BiomeData(Overworld.Biome.Volcano)
            {
                GrassVoxel = "Stone",
                SoilVoxel = "Stone",
                SubsurfVoxel = "Stone",
                ShoreVoxel = "Sand"
            };
            vocano.Motes.Add(dead);
            Biomes[Overworld.Biome.Volcano] = vocano;
        }
    }

}