# DwarfCorp

![](https://github.com/CompletelyFairGames/dwarfcorp/blob/master/DwarfCorp/DwarfCorpContent/Logos/gamelogo.png)

[DwarfCorp](www.dwarfcorp.com) from [Completely Fair Games](www.completelyfairgames.com) is a single player tycoon/strategy game for PC. In the game, the player manages a corporate colony of dwarves. The dwarves must mine resources, build structures, and contend with the natives to survive.

## External Dependencies
To develop DwarfCorp, you need the following libraries. If you just want to play the game, download one of the packages on our releases page.

* [The XNA 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=23714) library (not included)
* Note: If you are running Windows 8 or higher, you will need to download [MXA](https://mxa.codeplex.com/) instead.
* [XNA-FNA](https://github.com/FNA-XNA/FNA) for cross-platform development (forked)
* [LibNoise.NET](https://libnoisedotnet.codeplex.com/) (source code included)
* [JSON.NET](https://github.com/JamesNK/Newtonsoft.Json) (source code included)

## Building

To build and run in the game on a windows PC, you must do the following:

### Building for XNA on Windows
1. Download and install the XNA Game Studio 4.0 library (or [MXA](https://mxa.codeplex.com/) if you are running Windows 8 or higher)
2. Download and install Visual Studio. The project files were created for Visual Studio Professional 2013. Earlier versions may not work. "Express" versions may also not work.
3. Open `DwarfCorp.sln` in Visual Studio
4. Set `DwarfCorpXNA` as the `StartUp` project.
5. Add references to `XNA` binaries to the `DwarfCorpXNA` project. They may also need to be added to the `DwarfCorpCore` project. 
6. Build `LibNoise` and `Json.Net` projects
7. Set the `DwarfCorpXNA` build mode to `Release` or `Debug`
8. Build `DwarfCorpXNA`

### Building for FNA on Windows
1. Open 'DwarfCorpFNA.sln' in Visual Studio
2. Build for XNA first. This will create the content files needed by FNA. Do this from within DwarfCorpFNA.sln
3. Set 'DwarfCorpFNA' as the 'StartUp' project.
4. Build DwarfCorpFNA.

## Project Structure
There are several projects under the main folder:

* **DwarfCorpCore** contains the core source code of the game, and is intended to be more-or-less platform independent. This is **not to be compiled.**
* **DwarfCorpXNA** contains source code for XNA builds of the game. Most source files here should just be symbolic links to **DwarfCorpCore**.
* **DwarfCorpFNA** Contains source code for the FNA build. Most of the files in here are symbolic links to **DwarfCorpCore** in a flat directory structure.
* **DwarfCorpContent** Contains images, sounds, music, and content configuration files for DwarfCorp. Most assets in this content project may not be redistributed.
* **LibNoise** Is a fork of LibNosie.NET noise generation library.
* **JSON.NET** Is a fork of the JSON.NET data serialization library.
* **FNA** is a fork of XNA-FNA.

## Licensing
The game is released under a modified MIT licensing agreement. That means all *source code* is free to use, modify and distribute. However, we have explicitly disallowed modification and redistribution of the following game content (which remains proprietary):

* Images/Textures
* 3D Models
* Sound Effects
* Music

No forks, binary redistributions, or other redistributions of this repository may include the proprietary game content. It is up to the redistributor to provide their own game content. "Source code" may also include raw text files, JSON library files, and XML configuration files (which are not considered proprietary "game content").

It's complicated. If you have a question about the licensing, raise an issue on the repository.
