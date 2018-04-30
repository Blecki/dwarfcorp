# DwarfCorp

![](https://github.com/CompletelyFairGames/dwarfcorp/blob/master/DwarfCorp/DwarfCorpContent/Logos/gamelogo.png)

[DwarfCorp](www.dwarfcorp.com) from [Completely Fair Games](www.completelyfairgames.com) is a single player tycoon/strategy game for PC. In the game, the player manages a corporate colony of dwarves. The dwarves must mine resources, build structures, and contend with the natives to survive.

## BEFORE READING
If you're a developer/programmer and have the technical chops to compile C# code on Windows, continue reading. Otherwise, if you just want to play the game, buy the game on [Steam](http://store.steampowered.com/app/252390/DwarfCorp/?beta=0) or [itch.io](https://completelyfairgames.itch.io/dwarfcorp). You can also try a build on our releases page (above), but these builds will not be updated after the weekend of September 23rd, 2017.

## External Dependencies
To develop DwarfCorp, you need the following libraries. If you just want to play the game, download one of the packages on our releases page.

* [The XNA 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=23714) library (not included)
* Note: If you are running Windows 8 or higher, you will need to use [MXA](https://github.com/CompletelyFairGames/dwarfcorp/wiki/Installing-MXA-Hacks) instead. This is included in our repository as a convenience. XNA has gotten very old and support was dropped for it long ago, and we must jump through hoops to get it working.
* [XNA-FNA](https://github.com/FNA-XNA/FNA) for cross-platform development (forked)
* [LibNoise.NET](https://libnoisedotnet.codeplex.com/) (source code included)
* [JSON.NET](https://github.com/JamesNK/Newtonsoft.Json) (source code not included. You should get this through nuget.)

## Cross Platform Development
It is not possible to develop the game on anything other than a Windows machine at the moment. The game is developed using XNA/FNA using the XNA content project, [which only supports a windows development environment](https://github.com/FNA-XNA/FNA/issues/126). That said, the game can be cross compiled for windows/mac using FNA, but only in windows in a Visual Studio environment.

## Building

To build and run in the game on a windows PC, you must do the following:

### Building for XNA on Windows
1. Download and install Visual Studio. Any version after 2012 will probably work. Our dev team currently uses Visual Studio 2017 Community.
2. Download and install the XNA Game Studio 4.0 library (or install [MXA](https://github.com/CompletelyFairGames/dwarfcorp/wiki/Installing-MXA-Hacks) from our repository if you are running Windows 8 or higher)

#### INSTALLING MXA ####
We started developing DwarfCorp in 2012. Back then, XNA was still a supported library in Visual Studio 2010. Ever since then, support has been dropped. So if you want to compile the game in say, Visual Studio 2017 Community, you will need to install the hacked up "MXA". This is a version of XNA that is hacked together to work on newer versions of Windows. We have included this in a folder called "MXA" under the root of our repository. Open that zip file, and install the dependencies one by one (there are 5 in the zip folder).

Note that in 2017, the MXA project seems to have disappeared itself. Newer versions of Visual Studio (2017 and higher) don't seem to work with it. Luckily, there is *another hack* that you can apply to MXA to get it working with Visual Studio 2017 here:

Note that we have included those hacks in our wiki here: https://github.com/CompletelyFairGames/dwarfcorp/wiki/Installing-MXA-Hacks

Original source is [here.](https://gist.github.com/roy-t/2f089414078bf7218350e8c847951255)

#### Important note about the MXA Hacks!! ####
These hacks will install `Microsoft.Build.Framework.dll` into the global assembly cache (`GAC`). Since the `GAC` is no longer used to install Visual Studio dependencies, **any upgrade you make to visual studio will break all of your projects** after applying this hack. To resolve this issue, you must *completely uninstall and reinstall visual studio* instead of upgrading it, and then reapply the MXA hacks to get DwarfCorp compiling.

#### Compiling Build in VS ####
Once you have Visual Studio and XNA (or MXA) installed, you're ready to compile DwarfCorp.

3. Open `DwarfCorp.sln` in Visual Studio
4. Right-click on `DwarfCorpXNA` and set it as a `StartUp` project
5. Add references to `XNA` binaries to the `DwarfCorpXNA` project. 
6. Build `LibNoise` project
7. Set the `DwarfCorpXNA` build mode to `Release` or `Debug`
8. Build `DwarfCorpXNA`
9. You're done! Launch the build!

### Problems and Solutions with building for XNA on Windows
* **I opened the solution but I don't see anything**
    View->Solution Explorer
* **I can't install xna on windows 10**
    * Install xna refresh https://mxa.codeplex.com/releases/view/618279
    * (you may find that google marks this download as malware. We do not endorse this binary but we have a programmer using it and virustotal seems to find no malware scanning the file)
* **I can't download from the mxa website**
    * If this happens, we've included a complete copy of MXA in our repository for you to use.
* **'Unable to find manifest signing certificate in the certificate store'**
    * Project->DwarfCorpXNA properties-> Signing-> Create Test Certificate 
    * You can use whatever password you want
    
### Building for FNA on Windows
1. Open 'DwarfCorpFNA.sln' in Visual Studio
2. Build for XNA first. This will create the content files needed by FNA. Do this from within DwarfCorpFNA.sln
3. Set 'DwarfCorpFNA' as the 'StartUp' project.
4. Build DwarfCorpFNA.

## Project Structure
There are several projects under the main folder:

* **DwarfCorpXNA** contains source code for XNA builds of the game.
* **DwarfCorpFNA** Contains source code for the FNA build. Most of the files in here are just cross-included from DwarfCorpXNA, but using different linked libraries and compile flags.
* **DwarfCorpContent** Contains images, sounds, music, and content configuration files for DwarfCorp. Most assets in this content project may not be redistributed without the consent of Completely Fair Games Ltd.
* **LibNoise** Is a fork of LibNosie.NET noise generation library.
* **FNA** is a fork of XNA-FNA.

## Licensing
The game is released under a modified MIT licensing agreement. That means all *source code* is free to use, modify and distribute. However, we have explicitly disallowed modification and redistribution of the following game content (which remains proprietary):

* Images/Textures
* 3D Models
* Sound Effects
* Music

No forks, binary redistributions, or other redistributions of this repository may include the proprietary game content. It is up to the redistributor to provide their own game content. "Source code" may also include raw text files, JSON library files, and XML configuration files (which are not considered proprietary "game content").

It's complicated. If you have a question about the licensing, raise an issue on the repository.
