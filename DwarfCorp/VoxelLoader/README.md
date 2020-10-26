# CsharpVoxReader
A generic C# reader for [MagicaVoxel](https://ephtracy.github.io/)'s vox file format. It should be usable with any C# project (monogame, unity, ...). If it doesn't, please let me know. Contributions are welcome.

## Usage
First clone the repository as a submodule for your current project.

Then you have to make your own implementation of `IVoxLoader` so that you can receive vox data from a file (palettes, models, ...).

```csharp
using CsharpVoxReader;
using System;


class MyLoader : IVoxLoader
{
    public void LoadModel(Int32 sizeX, Int32 sizeY, Int32 sizeZ, byte[,,] data)
    {
        // Create a model
    }

    public void LoadPalette(UInt32[] palette)
    {
        // Set palette

        // You can use extension method ToARGB from namespace CsharpVoxReader
        // To get rgba values
        // ie: palette[1].ToARGB(out a, out r, out g, out b)
    }
}
```

Finally pass it to `VoxReader` to read a vox file.

```csharp
using CsharpVoxReader;
using System;


class Program
{
    static void Main(string[] args)
    {
        VoxReader r = new VoxReader(@"C:\path\to\my\file.vox", new MyLoader());

        r.Read();
    }
}
```

## Note about vox file format
Magicavoxel was recently updated to v0.99a. Vox file format number was unchanged, but includes a lot of new stuff, not yet documented in the [Vox file format description](https://github.com/ephtracy/voxel-model).

As such, `CsharpVoxReader` can only read documented chunks : palettes and models (Material and Pack chunks are read from older magicavoxel versions, but material has changed and pack doesn't seem to be used anymore). New stuff like layers, model position in the world won't be readable until the documentated.

