# Death's Door Archipelago Randomizer Client

This is a work in progress Archipelago randomizer mod for Death's Door.

> [!IMPORTANT]
> This is in an early alpha stage! You may experience errors that disrupt play. Please report these errors in Issues here or in the Death's Door `future-game-design` thread on the Archipelago Discord (see [Contributing](#contributing) below).

## Installation
1. Install [BepInEx 5.4.22 or later 5.x versions](https://github.com/bepinex/bepinex/releases/latest)
    - Do **not** use BepInEx 6.x, as it's in pre-release stage and may not work properly or be compatible.
    - Unzip the BepInEx folder into the root of your game's installation directory (i.e. there should be a BepInEx folder in the same folder as your DeathsDoor.exe)
1. Run the game once to generate the BepInEx plugins folder, then quit before performing the next step
1. Install these mods into your Death's Door BepInEx plugins folder (inside the BepInEx folder) by unzipping the folder from the release. Note that each mod should be in its own folder within the plugins folder.
    - [Archipelago Randomizer](https://github.com/Chris-Is-Awesome/DDArchipelagoRandomizer/releases/latest) (required)
    - [Alternative Game Modes](https://github.com/dpinela/DeathsDoor.AlternativeGameModes) (required)
    - [Item Changer](https://github.com/dpinela/DeathsDoor.ItemChanger) (required)
    - [MagicUI](https://github.com/dpinela/DeathsDoor.MagicUI) (required)
    - [AddUIToOptionsMenu](https://github.com/roseasromeo/DeathsDoor.AddUIToOptionsMenu) (required)
    - [RecentItemsDisplay](https://github.com/dpinela/DeathsDoor.RecentItemsDisplay) (recommended, but not required)
    - [ReturnToSpawn](https://github.com/roseasromeo/DeathsDoorReturnToSpawn) (recommended, but not required)
1. Run the game. You should see at least two additional options (`Deathlink` and `Fast Items`) in your options menu, which will confirm that the mods have loaded correctly.

## Playing
1. Install the mods as instructed above.
1. Generate and host a world using the Death's Door apworld as instructed in the [Death's Door apworld](https://github.com/roseasromeo/DeathsDoorAPWorld) README.
1. After selecting Start on the Title Screen and selecting a new save file, you'll be able to navigate left and right on the "Start" button to choose "Archipelago." Select "Archipelago".
1. Enter in the connection details for your generated world and slot. If you generated a solo world with the default template, your Player Name is Player1.
1. If you entered in your details correctly and your room is currently open, then you will load into game.
1. As you play, you should see item notifications pop up in the bottom right of your screen as you send and receive items. Items you pick up for yourself will have slightly more delay because we wait to notify you until you have received it.
1. If playing over multiple sessions, you can resume your save by clicking Start or Archipelago on your existing save, and updating your connection information if the port changes.

### Mod Options
* Deathlink (Title Screen Options Menu)
  * If Deathlink is on, you will send a Deathlink out when you die in game and die when you receive a deathlink from another game with Deathlink turned on.
* Fast Items (Title Screen and In-Game Options Menus)
  * If Fast Items is on, then you will receive items as fast as the game will allow and notifications will be queued for viewing. If Fast Items is off, the items themselves will be queued for receipt.
* Skip Cutscenes (Title Screen Options Menus)
  * If Skip Cutscenes is on when a file is started, most skippable cutscenes will be skipped. If Skip Cutscenes is off, then only a limited number of cutscenes that can block progress will be skipped.

## Contributing

If you're interested in helping out, join the discussion in the [Archipelago Discord server](https://discord.com/invite/8Z65BR2).
We talk about the development of this in the Death's Door thread in the `future-game-design` forum!

### Prerequisites
- [.NET Framework 4.7.2 or later](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- [BepInEx 5.4.22 or later 5.x versions](https://github.com/bepinex/bepinex/releases/latest)
  - Do **not** use BepInEx 6.x, as it's in pre-release stage and may not work properly or be compatible.
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)

### Building
1. Clone this repository.
1. Install these mods into your Death's Door plugins folder:
    - [Alternative Game Modes](https://github.com/dpinela/DeathsDoor.AlternativeGameModes) (required)
    - [Item Changer](https://github.com/dpinela/DeathsDoor.ItemChanger) (required)
    - [MagicUI](https://github.com/dpinela/DeathsDoor.MagicUI) (required)
    - [AddUIToOptionsMenu](https://github.com/roseasromeo/DeathsDoor.AddUIToOptionsMenu) (required)
    - [RecentItemsDisplay](https://github.com/dpinela/DeathsDoor.RecentItemsDisplay) (recommended, but not required)
    - [ReturnToSpawn](https://github.com/roseasromeo/DeathsDoorReturnToSpawn) (recommended, but not required)
1. Create a new file at the project's root named `config.targets` and add the following code:
    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <Project>
      <PropertyGroup>
        <PluginsPath>Path\To\Your\Death's\Door\Plugins\Directory</PluginsPath>
      </PropertyGroup>
    </Project>
    ```
    Replace `Path\To\Your\Death's\Door\Plugins\Directory`.
    This will usually be at `Game directory]\BepInEx\plugins`, unless you changed the default location.
1. Navigate to the project's root directory in a terminal and run `dotnet restore` to install packages.
1. Build the project.

### Code Guidelines
- Follow the [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) *except for the following*:
  - **Use 4 space tabs** for indentation.
    - This should be set up for you in the `.editorconfig` file.
  - **Never use implicit typing** (var) for variables.
    - This should be set up for you in the `.editorconfig` file.
- If you add a new mod dependency, do the following:
  - Include it in `Plugin`'s `BepInDepndency` attributes.
  - Edit the `.csproj` to add it as a reference, but use dynamic pathing for it using the `PluginsPath` custom MSBuild variable.
	- This ensures the mod is included in the project for anyone who builds it, regardless of difference in paths.
	- Example: `<HintPath>$(PluginsPath)/ItemChanger/ItemChanger.dll</HintPath>`

## Acknowledgements

Thanks to [dpinela](https://github.com/dpinela) for their work on the
[Multiworld Randomizer](https://github.com/dpinela/DeathsDoor.Randomizer)
and for some of the dependencies this mod uses:
[Alternative Game Modes](https://github.com/dpinela/DeathsDoor.AlternativeGameModes),
[Item Changer](https://github.com/dpinela/DeathsDoor.ItemChanger), and [MagicUI](https://github.com/dpinela/DeathsDoor.MagicUI)!

Thanks to [lunix33](https://github.com/lunix33) for starting this project with their work on the
first attempt at an [APWorld](https://github.com/lunix33/Archipelago_DeathsDoor/tree/deaths-door/worlds/deaths_door) side of things!

Thanks to [BadMagic](https://github.com/BadMagic100) for their work on
[Hollow Knight's Archipelago client](https://github.com/ArchipelagoMW-HollowKnight/Archipelago.HollowKnight),
which was used as a reference for the MSBuild properties of this project, and for the original version of MagicUI!

Thanks to [Silent](https://github.com/silent-destroyer) and [Scipio Wright](https://github.com/ScipioWright)
for their work on [Tunic's Archipelago client](https://github.com/ScipioWright/tunic-randomizer-archipelago-ER),
which was used as a reference for some of the Archipelago integration!
