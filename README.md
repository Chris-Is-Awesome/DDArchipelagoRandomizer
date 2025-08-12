# Death's Door Archipelago Randomizer Client

This is a work in progress Archipelago randomizer mod for Death's Door.

> [!IMPORTANT]
> This is in very early alpha stage and is not yet playable. A playable version will be coming soon!

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
(of which I'm using to figure out how to make this mod) and for the dependencies this mod uses:
[Alternative Game Modes](https://github.com/dpinela/DeathsDoor.AlternativeGameModes),
[Item Changer](https://github.com/dpinela/DeathsDoor.ItemChanger), and [MagicUI](https://github.com/dpinela/DeathsDoor.MagicUI)!

Thanks to [lunix33](https://github.com/lunix33) for starting this project with their work on the
[APWorld](https://github.com/lunix33/Archipelago_DeathsDoor/tree/deaths-door/worlds/deaths_door) side of things!

Thanks to [BadMagic](https://github.com/BadMagic100) for their work on
[Hollow Knight's Archipelago client](https://github.com/ArchipelagoMW-HollowKnight/Archipelago.HollowKnight),
which I used as a reference for the MSBuild properties of this project, and for their answering my questions!

Thanks to [Silent](https://github.com/silent-destroyer) and [Scipio Wright](https://github.com/ScipioWright)
for their work on [Tunic's Archipelago client](https://github.com/ScipioWright/tunic-randomizer-archipelago-ER),
which I used as a reference for some of the Archipelago integration!