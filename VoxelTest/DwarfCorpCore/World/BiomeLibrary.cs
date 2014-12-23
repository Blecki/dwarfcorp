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
            VegetationData grasslandPines = new VegetationData("Pine Tree", 0.5f, 0.5f, 1.7f, 25, 0.8f, 0.01f);
            VegetationData grasslandBushes = new VegetationData("Berry Bush", 1.0f, 0.1f, 0.6f, 30, 0.8f, 0.02f);
            VegetationData grasslandWheat = new VegetationData("Wheat", 1.0f, 1.0f, 0.45f, 50, 0.8f, 0.01f);
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
            grassland.Vegetation.Add(grasslandWheat);
            grassland.Vegetation.Add(grasslandPines);
            grassland.Vegetation.Add(grasslandBushes);
            grassland.Motes.Add(greenGrass);
            grassland.Motes.Add(flowers);
            grassland.Fauna.Add(grasslandBirds);
            Biomes[Overworld.Biome.Grassland] = grassland;

            VegetationData forestPines = new VegetationData("Pine Tree", 1.0f, 0.5f, 1.7f, 30, 0.5f, 0.02f);
            VegetationData forestBushes = new VegetationData("Berry Bush", 1.0f, 0.1f, 0.6f, 25, 0.8f, 0.01f);
            VegetationData bigForestMushroom = new VegetationData("Mushroom", 1, 1, 0.45f, 35, 0.8f, 0.02f);
            DetailMoteData forestGrass = new DetailMoteData("grass", ContentPaths.Entities.Plants.grass, 0.1f, 0.6f, 1.0f);
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
            forest.Vegetation.Add(bigForestMushroom);
            forest.Vegetation.Add(forestBushes);
            forest.Motes.Add(forestGrass);
            forest.Fauna.Add(forestBirds);
            forest.Fauna.Add(forestDeer);
            Biomes[Overworld.Biome.Forest] = forest;

            VegetationData snowPines = new VegetationData("Snow Pine Tree", 1.0f, 0.5f, 1.7f, 25.0f, 0.9f, 0.01f);
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

            VegetationData desertPalms = new VegetationData("Palm Tree", 0.5f, 0.5f, 1.7f, 100.0f, 0.8f, 0.01f);
            VegetationData desertWheat = new VegetationData("Wheat", 0.5f, 0.1f, 0.45f, 60.0f, 0.8f, 0.02f);
            DetailMoteData brownGrass = new DetailMoteData("gnarled", ContentPaths.Entities.Plants.gnarled, 0.1f, 0.6f, 1.0f);
            DetailMoteData dead = new DetailMoteData("deadbush", ContentPaths.Entities.Plants.gnarled, 0.5f, 0.9f, 1.0f);
            BiomeData desert = new BiomeData(Overworld.Biome.Desert)
            {
                GrassVoxel = "DesertGrass",
                SoilVoxel = "Sand",
                ShoreVoxel = "Sand",
                SubsurfVoxel = "Stone",
                ClumpGrass = true
            };
            desert.Vegetation.Add(desertWheat);
            desert.Motes.Add(brownGrass);
            desert.Motes.Add(dead);
            desert.Vegetation.Add(desertPalms);
            Biomes[Overworld.Biome.Desert] = desert;


            VegetationData junglePines = new VegetationData("Palm Tree", 2.0f, 1.0f, 1.7f, 10.0f, 0.5f, 0.03f);
            VegetationData jungleBushes = new VegetationData("Berry Bush",  1.0f, 0.1f, 0.6f, 30.0f, 0.8f, 0.02f);
            VegetationData jungleMushrooms = new VegetationData("Mushroom", 1.0f, 1.0f, 0.45f, 20.0f, 0.85f, 0.01f);
            DetailMoteData jungleGrass = new DetailMoteData("vine", ContentPaths.Entities.Plants.vine, 0.1f, 0.6f, 1.0f);
            FaunaData junglebirds = new FaunaData("Bird", 0.001f);
            BiomeData jungle = new BiomeData(Overworld.Biome.Jungle)
            {
                GrassVoxel = "JungleGrass",
                SoilVoxel = "Dirt",
                SubsurfVoxel = "Stone",
                ShoreVoxel = "Sand"
            };
            jungle.Vegetation.Add(jungleMushrooms);
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