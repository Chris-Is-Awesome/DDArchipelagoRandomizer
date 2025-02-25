# Death's Door Archipelago Randomizer

This is a work in progress Archipelago randomizer mod for Death's Door.

> [!IMPORTANT]
> This is in very early alpha stage and is not yet playable.

## Contributing

If you're interested in helping out, join the discussion in the [Archipelago Discord server](https://discord.com/invite/8Z65BR2).
We talk about the development of this in the Death's Door thread in the `future-game-design` forum!

### Prerequisites
- [.NET Framework 4.7.2 or later](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- [BepInEx 5.4.22 or later 5.x versions](https://github.com/bepinex/bepinex/releases/latest)
  - Do **not** use BepInEx 6.x, as it's in pre-release stage and may not work properly or be compatible.

### Building
1. Clone this repository.
1. Install these mods into your Death's Door plugins folder:
    - [Alternative Game Modes](https://github.com/dpinela/DeathsDoor.AlternativeGameModes)
    - [Item Changer](https://github.com/dpinela/DeathsDoor.ItemChanger)
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
    This will usually be at [`Game directory]\BepInEx\plugins`, unless you changed the default location.
1. Navigate to the project's root directory in a terminal and run `dotnet restore` to install packages.
1. Build the project.

## Acknowledgements

Thanks to [dpinela](https://github.com/dpinela) for their work on the
[Multiworld Randomizer](https://github.com/dpinela/DeathsDoor.Randomizer)
(of which I'm using to figure out how to make this mod) and for the dependencies this mod uses:
[Alternative Game Modes](https://github.com/dpinela/DeathsDoor.AlternativeGameModes) and
[Item Changer](https://github.com/dpinela/DeathsDoor.ItemChanger)!

Thanks to [lunix33](https://github.com/lunix33) for starting this project with their work on the
[APWorld](https://github.com/lunix33/Archipelago_DeathsDoor/tree/deaths-door/worlds/deaths_door) side of things!

Thanks to [BadMagic](https://github.com/BadMagic100) for their work on
[Hollow Knight's Archipelago client](https://github.com/ArchipelagoMW-HollowKnight/Archipelago.HollowKnight),
which I used as a reference for the MSBuild properties of this project.