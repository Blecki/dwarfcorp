# DwarfCorp

![](https://github.com/CompletelyFairGames/dwarfcorp/blob/master/DwarfCorp/DwarfCorpContent/Logos/gamelogo.png)

[DwarfCorp](www.dwarfcorp.com) from [Completely Fair Games](www.completelyfairgames.com) is a single player tycoon/strategy game for PC. In the game, the player manages a corporate colony of dwarves. The dwarves must mine resources, build structures, and contend with the natives to survive.

## BEFORE READING
If you're a developer/programmer and have the technical chops to compile C# code on Windows, continue reading. Otherwise, if you just want to play the game, download a binary package [from our website](www.dwarfcorp.com/site/download).

## External Dependencies
To develop DwarfCorp, you need the following libraries. If you just want to play the game, download one of the packages on our releases page.

* [The XNA 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=23714) library (not included)
* Note: If you are running Windows 8 or higher, you will need to download [MXA](https://mxa.codeplex.com/) instead.
* [XNA-FNA](https://github.com/FNA-XNA/FNA) for cross-platform development (forked)
* [LibNoise.NET](https://libnoisedotnet.codeplex.com/) (source code included)
* [JSON.NET](https://github.com/JamesNK/Newtonsoft.Json) (source code not included)

## Cross Platform Development
It is not possible to develop the game on anything other than a Windows machine at the moment. The game is developed using XNA/FNA, which only supports a windows development environment. That said, the game can be cross compiled for windows/mac using FNA, but only in windows in a Visual Studio environment.

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

### Problems and Solutions with building for XNA on Windows
* **I opened the solution but I don't see anything**
    View->Solution Explorer
* **I can't install xna on windows 10**
    * Install xna refresh https://mxa.codeplex.com/releases/view/618279
    * (you may find that google marks this download as malware. We do not endorse this binary but we have a programmer using it and virustotal seems to find no malware scanning the file)
* **I can't download from the mxa website because chrome says it's malware**
    * Use another browser or temporarily disable malware/phishing protection on chrome
* **'Unable to find manifest signing certificate in the certificate store'**
    * Project->DwarfCorpXNA properties-> Signing-> Create Test Certificate 
    * You can use whatever password you want
* **'Cannot find wrapper assembly for type library "Microsoft.Office.Core"'**
    I think this is error is caused if you don't have Microsoft Office.
    1. Remove broken Microsoft.Office.Core reference.
        * In the Solution Explorer tab under DwarfCorpXNA expand References. Right click 'Microsoft.Office.Core' and select Remove
    2. Add working Microsoft.Office.Core reference.
        * Project->Add Reference..->COM 
        * search "microsoft office" which should turn up 'Microsoft Office 15.0 Object Library'. Add that and press OK
* **'Could not find file 'LumenWorks.Framework.IO.dll'**
    * Project->Manage NuGet Packages..->search for 'LumenWorks.Framework'
    * This should turn up 'LumenWorks.Framework.IO'. Click Install. If given a prompt, just check DwarfCorpXNA.
    * In the Solution Explorer tab under DwarfCorpXNA you will find LumenWorks.Framework.IO. Delete this file. Note: There will be reference to LumenWorks.Framework.IO under References. Do /not/ delete that reference.
    

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
