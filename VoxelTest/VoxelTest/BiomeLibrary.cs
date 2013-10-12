using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class BiomeLibrary
    {
        public static Dictionary<Overworld.Biome, BiomeData> Biomes = new Dictionary<Overworld.Biome, BiomeData>();


        public BiomeLibrary()
        {
            InitializeStatics();
        }


        public static void InitializeStatics()
        {
            VegetationData GrasslandPines = new VegetationData("pine", 0.001f, 0.5f, 0.5f, 1.7f);
            VegetationData GrasslandBushes = new VegetationData("berrybush", 0.005f, 1.0f, 0.1f, 0.6f);
            DetailMoteData greenGrass = new DetailMoteData("Grassthing", 0.1f, 0.6f, 1.0f);
            DetailMoteData flowers = new DetailMoteData("flower", 0.3f, 0.9f, 0.8f);
            BiomeData Grassland = new BiomeData(Overworld.Biome.Grassland);
            Grassland.GrassVoxel = "Grass";
            Grassland.SoilVoxel = "Dirt";
            Grassland.SubsurfVoxel = "Stone";
            Grassland.ShoreVoxel = "Sand";
            Grassland.Vegetation.Add(GrasslandPines);
            Grassland.Vegetation.Add(GrasslandBushes);
            Grassland.Motes.Add(greenGrass);
            Grassland.Motes.Add(flowers);
            Biomes[Overworld.Biome.Grassland] = Grassland;

            VegetationData forestPines = new VegetationData("pine", 0.008f, 1.0f, 0.5f, 1.7f);
            VegetationData forestBushes = new VegetationData("berrybush", 0.004f, 1.0f, 0.1f, 0.6f);
            DetailMoteData forestGrass = new DetailMoteData("Grassthing", 0.1f, 0.6f, 1.0f);
            DetailMoteData forestMushrooms = new DetailMoteData("shroom", 0.5f, 0.9f, 0.8f);
            BiomeData forest = new BiomeData(Overworld.Biome.Forest);
            forest.GrassVoxel = "Grass";
            forest.SoilVoxel = "Dirt";
            forest.SubsurfVoxel = "Stone";
            forest.ShoreVoxel = "Sand";
            forest.Vegetation.Add(forestPines);
            forest.Vegetation.Add(forestBushes);
            forest.Motes.Add(forestGrass);
            forest.Motes.Add(forestMushrooms);
            Biomes[Overworld.Biome.Forest] = forest;

            VegetationData snowPines = new VegetationData("snowpine", 0.005f, 1.0f, 0.5f, 1.7f);
            DetailMoteData blueGrass = new DetailMoteData("FrostGrass", 0.1f, 0.6f, 1.0f);
            BiomeData taiga = new BiomeData(Overworld.Biome.ColdForest);
            taiga.GrassVoxel = "Frost";
            taiga.SoilVoxel = "Dirt";
            taiga.SubsurfVoxel = "Stone";
            taiga.ShoreVoxel = "Sand";
            taiga.Vegetation.Add(snowPines);
            taiga.Motes.Add(blueGrass);
            Biomes[Overworld.Biome.ColdForest] = taiga;

            BiomeData tundra = new BiomeData(Overworld.Biome.Tundra);
            tundra.GrassVoxel = "Frost";
            tundra.SoilVoxel = "Dirt";
            tundra.SubsurfVoxel = "Stone";
            tundra.ShoreVoxel = "Sand";
            tundra.Motes.Add(blueGrass);
            Biomes[Overworld.Biome.Tundra] = tundra;

            VegetationData desertPalms = new VegetationData("palm", 0.0005f, 0.5f, 0.5f, 1.7f);
            DetailMoteData brownGrass = new DetailMoteData("gnarled", 0.1f, 0.6f, 1.0f);
            DetailMoteData dead = new DetailMoteData("deadbush", 0.5f, 0.9f, 1.0f);
            BiomeData desert = new BiomeData(Overworld.Biome.Desert);
            desert.GrassVoxel = "Sand";
            desert.SoilVoxel = "Sand";
            desert.ShoreVoxel = "Sand";
            desert.SubsurfVoxel = "Stone";
            desert.Motes.Add(brownGrass);
            desert.Motes.Add(dead);
            desert.Vegetation.Add(desertPalms);
            Biomes[Overworld.Biome.Desert] = desert;


            VegetationData junglePines = new VegetationData("palm", 0.008f, 2.0f, 1.0f, 1.7f);
            VegetationData jungleBushes = new VegetationData("berrybush", 0.005f, 1.0f, 0.1f, 0.6f);
            DetailMoteData jungleGrass = new DetailMoteData("vine", 0.1f, 0.6f, 1.0f);
            BiomeData jungle = new BiomeData(Overworld.Biome.Jungle);
            jungle.GrassVoxel = "Grass";
            jungle.SoilVoxel = "Dirt";
            jungle.SubsurfVoxel = "Stone";
            jungle.ShoreVoxel = "Sand";
            jungle.Vegetation.Add(junglePines);
            jungle.Vegetation.Add(jungleBushes);
            jungle.Motes.Add(jungleGrass);
            Biomes[Overworld.Biome.Jungle] = jungle;


            BiomeData vocano = new BiomeData(Overworld.Biome.Volcano);
            vocano.GrassVoxel = "Stone";
            vocano.SoilVoxel = "Stone";
            vocano.SubsurfVoxel = "Stone";
            vocano.ShoreVoxel = "Sand";
            vocano.Motes.Add(dead);
            Biomes[Overworld.Biome.Volcano] = vocano;


        }
    }
}
