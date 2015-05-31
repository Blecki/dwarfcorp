# DwarfCorp

![](https://github.com/CompletelyFairGames/dwarfcorp/blob/master/DwarfCorp/DwarfCorpContent/Logos/gamelogo.png)

[DwarfCorp](www.dwarfcorp.com) from [Completely Fair Games](www.completelyfairgames.com) is a single player tycoon/strategy game for PC. In the game, the player manages a corporate colony of dwarves. The dwarves must mine resources, build structures, and contend with the natives to survive.

## External Dependencies
DwarfCorp depends on the following libraries:

* [The XNA 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=23714) library (not included)
* [LibNoise.NET](https://libnoisedotnet.codeplex.com/) (source code included)
* [JSON.NET](https://github.com/JamesNK/Newtonsoft.Json) (not included)

## Building

To build and run in the game on a windows PC, you must do the following:

1. Download and install the XNA Game Studio 4.0 library
2. Download and install Visual Studio. The project files were created for Visual Studio Professional 2013. Earlier versions may not work. "Express" versions may also not work.
3. Open `DwarfCorp.sln` in Visual Studio
4. Set `DwarfCorpXNA` as the `StartUp` project.
5. Add references to `XNA` and `Json.NET` binaries to the `DwarfCorpXNA` project. They may also need to be added to the `DwarfCorpCore` project. 
6. Set the build mode to `Release` or `Debug`
7. Hit `Build Project`

## Project Structure
There are several projects under the main folder:

* **DwarfCorpCore** contains the core source code of the game, and is intended to be more-or-less platform independent.
* **DwarfCorpXNA** contains source code for XNA builds of the game. Most source files here should just be symbolic links to **DwarfCorpCore**.
* **DwarfCorpMono** Contains source code for a MonoGame build. It hasn't been updated in a very long time, and will likely not build for now.
* **DwarfCorpContent** Contains images, sounds, music, and content configuration files for DwarfCorp. Most assets in this content project may not be redistributed.
* **LibNoise** Is a fork of LibNosie.NET noise generation library.

## Licensing
The game is released under a modified MIT licensing agreement. That means all *source code* is free to use, modify and distribute. However, we have explicitly disallowed modification and redistribution of the following game content (which remains proprietary):

* Images/Textures
* 3D Models
* Sound Effects
* Music

No forks, binary redistributions, or other redistributions of this repository may include the proprietary game content. It is up to the redistributor to provide their own game content. "Source code" may also include raw text files, JSON library files, and XML configuration files (which are not considered proprietary "game content").

It's complicated. If you have a question about the licensing, raise an issue on the repository.
