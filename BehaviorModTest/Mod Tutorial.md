
# DwarfCorp mods
## Where are mods located?
		
Mods downloaded from steam will be placed in your steam folder under `/steamapps/workshop/content/252390`, where that number if the DwarfCorp app id. You can change this in the steam client, but if you do so, you should also change it in your settings. This setting is not available from the options screen - try looking in your appdata or relevant folder for your settings file.
The full path may be `C:/Program Files/Steam/steamapps/workshop/content/252390` for default installations.
This is where any mods you subscribe to will be placed.

DwarfCorp will also load mods from the DwarfCorp install folder if they are placed in the sub folder `/Mods/NameOfMod/`.

## Mod meta data

Every mod must have a meta.json file or it will not be loaded. This is an example from the Manalamp mod.

```json
		<<meta.json>>
		{
			"Name": "Mana Lamp",
			"Description": "Adds a new entity called the Mana Lamp. This is a test.",
			"PreviewURL": "preview.png",
			"Tags": [
				"Mod",
				"Entity",
				"Test"
			],
			"ChangeNote": "First change",
			"SteamID": 1479398811
		}
		<<end meta.json>>
```

Most settings are self-explanatory. 

If you are creating a new mod, remove the 'SteamID' line, or set it equal to zero. Failure to do so will cause your mod to try and overwrite an existing mod. If you don't own the existing mod, this will fail.

## Uploading to steam

In order to upload your mod, it must be in the /Mods/ folder where DwarfCorp is installed. The game will assume any mod in the steam folder was downloaded from steam and will not allow you to upload it. If your mod does not already have a steam id, one will be assigned to it and will be saved in meta.json.

To upload, start DwarfCorp and navigate to the manage mods screen. On the 'installed' tab, any mod located in the /Mods/ folder will have an 'upload' or 'update' button.

## Subscribing

Under the 'search' tab of the manage mods screen, you can search for mods by keyword and subscribe to them. Once subscribed, the game will allow steam to download the mod in the background.
