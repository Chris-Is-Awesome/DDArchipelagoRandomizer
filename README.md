# Death's Door Archipelago

Death's Door is a game about being a crow whose job it is to collect souls, who then accidentally stumbles into a grand conspiracy about the nature of life and death. Also, you hit things with weapons.

This repo holds both halves of the [Archipelago](https://archipelago.gg/) randomizer for [Death's Door](https://store.steampowered.com/app/894020/Deaths_Door/):

- **The mod** (ArchipelagoRandomizer), a BepInEx mod for the game that connects to an Archipelago server. Its source is in the `ArchipelagoRandomizer` folder.
- **The apworld** (`deaths_door.apworld`), which tells Archipelago how to generate Death's Door worlds. Its source is in the `APWorld/deaths_door` folder.

Together, they take the things your character would pick up throughout the game and randomize them, potentially between multiple games. If you are new to Archipelago, start with Archipelago's [FAQ](https://archipelago.gg/faq/en/).

Use the mod and the apworld from the same [release](https://github.com/Chris-Is-Awesome/DDArchipelagoRandomizer/releases/latest).

> [!IMPORTANT]
> This is in a stable beta stage! If you do experience errors that disrupt play, please report these errors in Issues here or in the Death's Door thread of the `Custom-games` forum on the Archipelago Discord (see [Contributing](#contributing) below).

## Contents
- [Setup](#setup)
  - [1. Install the mod](#1-install-the-mod)
  - [2. Install Archipelago and the apworld](#2-install-archipelago-and-the-apworld)
  - [3. Create your yaml](#3-create-your-yaml)
  - [4. Generate and host](#4-generate-and-host)
  - [5. Play](#5-play)
  - [Recommendations](#recommendations)
  - [Troubleshooting](#troubleshooting)
- [Mod Options](#mod-options) (in-game settings)
  - [Door and stat tracker](#door-and-stat-tracker)
- [yaml Options](#yaml-options) (generation settings)
  - [DeathLink Options](#deathlink-options)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

## Setup

### 1. Install the mod
1. Install [BepInEx 5.4.22 or a later 5.x version](https://github.com/bepinex/bepinex/releases/latest).
    - Download `BepInEx_win_x64_<version>.zip` under **Assets** (on Linux/Steam Deck with Proton, use the same Windows x64 zip).
    - Do **not** use BepInEx 6.x, as it's in pre-release and may not work properly.
    - Unzip the BepInEx download into the game's installation folder, so that there is a `BepInEx` folder next to `DeathsDoor.exe`.
1. Run the game once to create the `BepInEx/plugins` folder, then quit.
1. Download each of these mods from its release page and unzip it into the `BepInEx/plugins` folder, each mod in its own folder.

    > [!IMPORTANT]
    > On each release page, download the `.zip` under **Assets**. The green **Code** button and the "Source code" zips only contain source code, which the game can't load.

    | Mod | Needed? | Download |
    |---|---|---|
    | Archipelago Randomizer | required | [release page](https://github.com/Chris-Is-Awesome/DDArchipelagoRandomizer/releases/latest) (`ArchipelagoRandomizer-<version>.zip`) |
    | Item Changer | required | [release page](https://github.com/dpinela/DeathsDoor.ItemChanger/releases/latest) |
    | Alternative Game Modes | required | [release page](https://github.com/dpinela/DeathsDoor.AlternativeGameModes/releases/latest) |
    | MagicUI | required | [release page](https://github.com/dpinela/DeathsDoor.MagicUI/releases/latest) |
    | AddUIToOptionsMenu | required | [release page](https://github.com/roseasromeo/DeathsDoor.AddUIToOptionsMenu/releases/latest) |
    | RecentItemsDisplay | recommended | [release page](https://github.com/dpinela/DeathsDoor.RecentItemsDisplay/releases/latest) |
    | ReturnToSpawn | recommended | [release page](https://github.com/roseasromeo/DeathsDoorReturnToSpawn/releases/latest) |

    When you're done, `BepInEx/plugins` should contain the folders `ArchipelagoRandomizer`, `ItemChanger`, `AlternativeGameModes`, `MagicUI` and `AddUIToOptionsMenu` (plus `RecentItemsDisplay` and `ReturnToSpawn` if you took them), each with a `.dll` file directly inside, not in another folder.
1. Run the game. You should see new options such as `Deathlink` and `Fast Items` in the Options menu, which confirms the mods have loaded.
    - If they're missing, open `BepInEx/LogOutput.log`. A line saying `Could not load [ArchipelagoRandomizer ...] because it has missing dependencies` means one of the required mods above isn't installed (or is a source code zip).

### 2. Install Archipelago and the apworld
1. Install [Archipelago 0.6.7 or later](https://github.com/ArchipelagoMW/Archipelago/releases/) (or follow their instructions to run from source).
1. Download `deaths_door.apworld` from the same [release](https://github.com/Chris-Is-Awesome/DDArchipelagoRandomizer/releases/latest) as the mod.
    - Make sure the file is named exactly `deaths_door.apworld`, without anything your browser may have added, such as `(1)`.
    - To build the apworld yourself, zip the `APWorld/deaths_door` folder of this repo into a file named `deaths_door.apworld`. The zip must contain the `deaths_door` folder at the top level.
1. On Windows, double-click the apworld to install it. It is copied into the `custom_worlds` folder of your Archipelago installation. On Linux and Mac, copy it into `custom_worlds` yourself.

Only the person generating the multiworld needs the apworld, but the [Universal Tracker](#recommendations) needs it too.

### 3. Create your yaml
1. Open the Archipelago Launcher and click `Generate Template Options`. On Windows, a File Explorer window opens with the template yamls. On other systems, they are in `Players/Templates` in your Archipelago installation.
1. Copy `Death's Door.yaml` and set the options you want. The Death's Door options are explained [below](#yaml-options). For the options every game shares, see Archipelago's [advanced yaml guide](https://archipelago.gg/tutorial/Archipelago/advanced_settings/en).
1. Set `name` at the top of the yaml. It needs to be different for every player in the multiworld (sorry, not everyone can be "CAAAAAAW").

### 4. Generate and host
1. Put your yaml and the yamls of the other players into the `Players` folder of your Archipelago installation.
    - Remove any yamls for people who are not playing this time. If you're playing one Death's Door world, there should be only one Death's Door yaml.
1. Click `Generate` in the Launcher.
1. Host the game from the `AP_<numbers>.zip` in the `output` folder, either with `Host` in the Launcher or by uploading it to [archipelago.gg/uploads](https://archipelago.gg/uploads).

### 5. Play
1. Before starting, open the Options menu on the Title Screen and check the [Mod Options](#mod-options), especially **Skip Cutscenes** and **TrapLink**. These settings are remembered between sessions, so check them again if something seems off.
1. Select Start on the Title Screen and choose a new save file. Move left or right on the "Start" button to choose "Archipelago", then select it.
1. Enter the connection details for your room and slot. If you generated a solo world with the default template, your Player Name is Player1.
1. If the details are correct and the room is open, the game loads.
1. As you play, notifications for items you send and receive pop up in the bottom right of the screen. Items you pick up for yourself take slightly longer, because the notification waits until you have received the item.
1. To continue over several sessions, select Start or Archipelago on your existing save, and update the connection details if the port has changed.

### Recommendations
- Use [Universal Tracker](https://github.com/FarisTheAncient/Archipelago/releases) to see which locations are in logic. It includes maps of every area. Universal Tracker needs the apworld installed.
  - To find your way with randomized entrances, type `/get_logical_path <place>` in Universal Tracker, for example `/get_logical_path Overgrown Ruins Door` or a location name. It lists the doors to take from the Hall of Doors, in order, and where each one leads, using only doors you have already been through.
- Keep the Text Client (from the Archipelago Launcher) open next to the game to see what you send and receive in more detail.
- In small multiworlds, leave `early_important_item` on `early` or change it to `local_early`. This makes generation less likely to fail.
- The mod's Stay Visible When Unfocused option (on by default) keeps the game on screen when you click over to the Text Client or tracker.

### Troubleshooting
- **No Archipelago options in the game, and `LogOutput.log` says `missing dependencies`**: one of the required mods from [step 1](#1-install-the-mod) is missing, or a "Source code" zip was used instead of the release zip under **Assets**. Each mod needs its own folder in `BepInEx/plugins` with its `.dll` directly inside.
- **Linux / Steam Deck (Proton): the game can't connect or stops after connecting.** The mod runs inside Proton, which keeps its own copy of Archipelago's data package cache. Copy the `datapackage` folder from your native Archipelago cache (for example `~/.local/Archipelago/cache/datapackage`) into the Proton prefix, at `steamapps/compatdata/894020/pfx/drive_c/users/steamuser/AppData/Local/Archipelago/Cache/datapackage/` (894020 is Death's Door on Steam), then start the game again.
- **"Connection timed out"**: check the address and port (the port changes whenever the room is restarted on archipelago.gg), that the room is still open, and that a firewall or antivirus isn't blocking the game.

## Mod Options
These are set in the game's Options menu. All of them are off by default, except Stay Visible When Unfocused. They are saved on your PC and stay as you left them for every save file and every future session, so check them before you start a new multiworld.

* Skip Cutscenes (Title Screen Options Menu) &mdash; **recommended: on**
  * If Skip Cutscenes is on when a file is started, most skippable cutscenes are skipped. If it is off, only a few cutscenes that can block progress are skipped.
  * Skip Cutscenes also skips the camera pans that show what you just opened, such as the Castle Lockstone braziers, the Lost Cemetery ghosts or crow souls flying into an Ancient Door.
  * A few cutscenes that can leave invisible walls behind are always skipped, whatever this setting says.
  * Either way, the Grove of Spirits Door check is sent as soon as you arrive in the Hall of Doors on a new file.
  * Most of these are decided when a new save file is started. The late-game story scenes are skipped whenever Skip Cutscenes is on, also on saves already under way: the Grey Crow opening Death's Door, the Grey Crow and Death in the void, Death's talk afterwards, the crows' scene in the Hall of Doors, and the Lord of Doors' speeches. The ending after the Lord of Doors and the credits still play.
* Deathlink (Title Screen Options Menu)
  * If Deathlink is on, you send a Deathlink when you die in game, and you die when you receive a Deathlink from another player with Deathlink on.
  * A received Deathlink waits until you can take damage (not during cutscenes, menus, rolls or invincibility frames), and the game shows who caused it. Deathlinks that arrive while you are dead or within 10 seconds of a death are ignored.
  * How forgiving Deathlink is comes from the yaml: see [DeathLink Amnesty and Death Wish](#deathlink-options).
* TrapLink (Title Screen Options Menu)
  * If TrapLink is on, traps that other TrapLink players receive are also sent to you (and yours to them). Traps from other games are converted to a Death's Door trap; for example a "Tiny Trap" makes you invisible for a while. They show up as received from the server. Leave this off unless your group plays with TrapLink.
* Fast Items (Title Screen and In-Game Options Menus)
  * If Fast Items is on, you receive items as fast as the game allows and their notifications are queued. If it is off, the items themselves are queued.
* Enemy Randomizer (Title Screen Options Menu)
  * If Enemy Randomizer is on, enemies are replaced by random other enemies.
* Stay Visible When Unfocused (Title Screen and In-Game Options Menus) &mdash; **on by default**
  * The game normally hides itself as soon as you click another window (for example the Text Client or tracker) until you click it again. With this on, it stays on screen behind the other window, or on its own monitor. The game still pauses its sound while it isn't focused, as normal.
  * It works by switching on Unity's "visible in background" setting in `DeathsDoor_Data/globalgamemanagers`, which the game only reads when it starts, so a change takes effect from the next start. Turning the option off switches it back. If Steam verifies or updates the game files, the mod simply switches it on again (again from the next start). If your copy of the game has a different data file (for example another store's version), the mod leaves it alone and writes a line about it in the BepInEx log.
* Fixed Random Enemies (Title Screen Options Menu)
  * With Enemy Randomizer on: if Fixed Random Enemies is on, every enemy is randomized the same way each time its area loads in that save, so respawning, dying, going through a loading zone or reloading the save doesn't change it. If it is off, enemies are rerolled every time an area loads. Different save files get different enemies.

### Door and stat tracker
While the game is paused, press Tab (keyboard) or Y (controller) to open a panel with:
* With entrance randomization on, the randomized doors of the area you are in and where each leads once you have been through it (with coupled doors, also the way back). Press Page Up / Page Down or the triggers to look at other areas.
* With Shop Upgrades on, the level of each stat you have received, next to how many of its upgrades you bought. The shop's bars show what you bought; your stats go up with the Progressive items you receive. The shop's description panel also shows your current level.

## yaml Options
These are set in your yaml before generating. The template yaml describes each option too.

### Logic Options
- **Goal**
    - Lord of Doors: defeat the Lord of Doors. Removes the Rusty Belltower Key location.
    - True Ending: receive all 7 Ancient Tablets of Knowledge and go to the door in the Camp of the Free Crows.
    - Green Tablet: open the door in the Estate of the Urn Witch Family Tomb by planting enough Life Seeds (set by Planted Pots Required). Removes the Green Tablet location.
    - Any: any of the above. On minimal accessibility, not every goal may be possible. Removes both the Rusty Belltower Key and the Green Tablet locations.
    - All: all three of the above, in any order. The game tells you which endings are still to do each time you finish one. Removes both the Rusty Belltower Key and the Green Tablet locations.
    - Holy Bird: visit all 16 Magic and Vitality Shard Shrines in your own world.
    - Lorekeeper: pick up all 24 Shiny Things (the lore items) in your own world. One of them is the Rusty Belltower Key, which you only get after beating the Lord of Doors, so Lorekeeper includes beating him.
    - Truthseeker: get all 7 Ancient Tablets of Knowledge in your own world: the six tablet locations and the three owls that make up the Pink Tablet.
        - For these three it's checking the locations in your own game that counts, not receiving those items. Their pool is always randomized, and the game shows your progress (for example "Holy Bird: 11/16 shrines").
    - Custom: you pick the goals in **Custom Goals** (Lord of Doors, True Ending, Green Tablet, Holy Bird, Lorekeeper, Truthseeker; checkboxes in the Options Creator), and in **Custom Goal Requirement** how many of them you need: `all`, `any` (one), or a number. For example Lord of Doors + Green Tablet + Holy Bird with `2` means any two of those three. Each chosen goal works like its own Goal option.
    - Any and All only cover the three endings (Lord of Doors, True Ending, Green Tablet); use Custom to combine the others.
- **Entrance Randomization**
    - Off, Coupled (going back through a door takes you where you came from) or Decoupled (it may not).
    - Use `plando_connections` to place particular connections yourself. See the template yaml for the format.
- **Start Day or Night**
    - Whether you start during the day or at night. You need to reach the Rusty Belltower Bell with the Rusty Belltower Key (or use the Bomb Bell Glitch) to switch.
- **Planted Pots Required**
    - How many Life Seeds you need to plant to open the Green Tablet door. Vanilla is 50, the default is 25. Every Life Seed in the pool counts in logic.
- **Early Important Item**
    - Whether one random important item is placed early in the multiworld (`early`), early in your own world (`local_early`), or anywhere (`random_placement`).
    - With `random_placement`, generation is more likely to fail.
    - Important items are all non-boss doors, Hookshot, Bomb and Fire (and the Reaper's Bow with Start With Bow off), since these open up the most early checks.
- **Gate Rolls Glitch**
    - Puts rolling through these gates in logic:
        - Estate entrance to the Crypt through the metal gate
        - Mushroom Dungeon Lobby to Overgrown Ruins through the vines
        - Mushroom Dungeon Ancient Door to the Lobby through a metal gate and a cobweb
- **Bomb Bell Glitch**
    - Puts hitting the Rusty Belltower bell with a Bomb from a railing, to switch between day and night, in logic. Also known as "Early Night".
- **Offscreen Targeting Tricks**
    - Puts three tricks in logic where you hit an enemy or switch from offscreen:
        - Open the switch between the Lost Cemetery and the Stranded Sailor caves with an Arrow shot through a fire source instead of Fire
        - Open a bomb wall in lower Overgrown Ruins by hitting an offscreen bomb flower with an Arrow
        - Open the path back from the Flooded Fortress Frog King Statue to the entrance by hitting a switch offscreen
- **Geometry Exploits**
    - Puts rolling on unintended surfaces in logic:
        - Roll across the walls near the ladder just outside the exit from the Lost Cemetery to the Stranded Sailor caves, to get around the switch on the Lost Cemetery side
        - Roll behind the grate in Castle Lockstone East Upper down to East
        - Roll onto the wall from the upper right platform in Castle Lockstone East Upper, after raising it, to reach the East Upper keyed door without the lever
        - Coming from the Old Watchtowers Barb Elevator, skip the lever to Ice Skating Start by hooking over the gate from the ledge at the top of the elevator
        - In Overgrown Ruins, reach the Soul Orb in Lower that needs Hookshot by rolling onto the wall above and dropping down. The standalone randomizer notes this roll may need Haste 2 or more.
        - In Overgrown Ruins, reach the Soul Orb in the Lord of Doors Hookshot arena, most likely the same way as above
- **Roll Buffers**
    - Puts roll buffers in logic, where you roll in mid-air after a heavy attack. Three can be done with any weapon but the Thunder Hammer, two only with the Rogue Daggers. This makes every weapon but the Thunder Hammer progression.
        - Hall of Doors - Surveillance Device: heavy attack to the right and roll down-right from behind the bin near the Discarded Umbrella check
        - Hall of Doors - Bomb Secret Soul Orb: same as above
        - Hall of Doors - Hookshot Secret Soul Orb: up-right heavy attack and roll from above the Lord of Doors poster by the staircase near the Bomb Avarice Chest (Rogue Daggers only)
        - Hall of Doors - Modern Door Scale Model: same as above (Rogue Daggers only)
        - Castle Lockstone - West Locked Crow: heavy attack off the ledge above the gate, then roll straight back

### Item Pool Options
- **Adjust Extra Life Seeds**
    - Adds Life Seeds to the pool (up to 20) or removes them (a negative number), counted from the 50 vanilla seeds. Added seeds replace Soul Orb items. When removing seeds, at least as many as Planted Pots Required are left.
- **Extra Magic Shards, Extra Vitality Shards**
    - Add up to 8 of each, replacing Soul Orb items. They can take your magic or health above the vanilla maximum of 6. Every 4 shards give one more pip.
- **Remove Spell Upgrades**
    - Leaves only 1 Fire, 1 Bomb and 1 Hookshot in the pool, and no Arrow upgrade.
- **Unrandomized Pools**
    - Keeps sets of locations (pools) at their vanilla items: Spell, Weapon, Giant Soul, Shrine, Shiny Thing, Life Seed, Soul Orb, Tablet, Lever, Door, Lost Crow. Keys are always randomized, since their vanilla locations can softlock.
    - The fewer pools are randomized, the more likely generation is to fail, especially in small multiworlds. Soul Orbs matter most here. See the template yaml for how each pool is handled.
- **Trap Chance, Trap Type Weights**
    - The chance (default 15%) for each filler item to become a trap, and how likely each kind of trap is: Rotation, Player Invisibility, Enemy Invisibility, Knockback and Frog King Title. Set Trap Chance to 0 for no traps.
    - The Frog King Title Trap plays the Frog King's boss intro title, with its sound and screen shake, and goes away by itself once it has finished.
- The Giant Soul of the Frog King has its full title as its name on the server. Use the item group `Giant Soul of the Frog King` to hint it, for example `!hint Giant Soul of the Frog King`.

### Shop and Planting
- **Shop Upgrades**
    - Turns the 20 stat upgrades sold by the Banker (5 each of Strength, Dexterity, Haste and Magic) into checks. Buying an upgrade sends a check; your stats go up when you receive Progressive Strength, Dexterity, Haste or Magic.
    - Logic expects the 1st to 5th upgrade of a stat once you can reach 1, 2, 3, 4 and 6 areas where you can farm souls from respawning enemies. The Hall of Doors, the Camp of the Free Crows and Castle Lockstone don't count; the Old Watchtowers count once you have the Hookshot, and the Stranded Sailor once you have the Bomb or Hookshot and have lit the lamp that opens the way from the Lost Cemetery. The Bankers in the Hall of Doors and the Camp of the Free Crows share the same stock.
- **Shop Prices**
    - Vanilla (400, 600, 800, 1000 and 1500 souls for each stat), Shuffled (the 20 vanilla prices shuffled between the stats) or Random Range (each price between Shop Price Minimum and Shop Price Maximum). Each stat's prices always rise from one upgrade to the next.
    - With Shop Upgrades off, this sets the prices of the normal shop.
- **Shop Hints** (on by default)
    - With Shop Upgrades on, looking at a stat in the shop shows the item its next upgrade holds and hints it, so it shows up in your hint list and the tracker, and the player it belongs to is told.
- **Shop Price Minimum, Shop Price Maximum**
    - The price range for Random Range (default 200 to 1500).
- **Extra Random Stat Upgrades**
    - Adds up to 20 extra Progressive stat items (random stats), replacing Soul Orb items. Stats can go above the vanilla maximum of 5 this way. Works with or without Shop Upgrades.
- **Extra Strength / Dexterity / Haste / Magic Upgrades**
    - Adds up to 10 extra Progressive items of that stat, on top of the random ones above.
- **Planting Checks, Seeds Per Planting Check**
    - Adds checks for planting Life Seeds in pots: one check each time the number of seeds planted reaches a multiple of Seeds Per Planting Check. For example, 10 checks with 5 seeds each are sent at 5, 10, ..., 50 seeds. 0 turns planting checks off.

### Customization Options
- **Starting Weapon**
    - The weapon you start with. The others are shuffled into the pool as useful items (or progression with Roll Buffers on).
    - The Umbrella is much worse than the other four. Choose it at your own risk.
    - If Weapons are unrandomized, you always start with the Reaper's Sword.
- **Start With Bow** (on by default)
    - Off: the Reaper's Bow is an item in the pool, and you can't aim or shoot arrows until you receive it. Fire, Bomb and Hookshot still work. The Arrow Upgrade does nothing without the bow.
    - Logic expects the bow (or Fire, where lighting something does the job) for:
        - Ceramic Manor without Fire: the After Spinny Pot Room Key, After Torch Puzzle Crow, After Dining Room Pot, Imp Loft Soul Orb, Pot and Crow, Under Dining Room Key and Library Key (arrows shot through burning fires), the Bedroom Crow and Pot (Hookshot, Bomb, Fire or the bow), and the Bookshelf Shortcut Lever, Library Crow and Library Pot (Fire, the bow or the Bookshelf Shortcut Lever)
        - the Estate of the Urn Witch: going on from the exit to the Crypt (the entrance lamps), the Garden of Joy Lever and Life Seed unless you have the Garden of Joy lever, and the Garden of Joy Pot unless you have the Bomb or that lever
        - the Garden of Love hidden pots, the Rusty Garden Trowel and the Green Tablet (bow only: the pot puzzle)
        - the way from the Grove of Spirits door to the rest of the Grove (any spell works there: the bow, Fire, Bomb or Hookshot)
        - going on from the Flooded Fortress entrance (from the Mushroom Dungeon side) to the Frog King Statue and beyond, which is open from the Flooded Fortress door
        - the Flooded Fortress Statue Life Seed, East After Statue Life Seed, Statue Lever, Drop Soul Orb, Watchtower Lower Lever, and everything in the watchtower behind the Watchtower Lower Lever (Watchtower Life Seed, Bomb Soul Orb, Watchtower Upper Lever, Hidden Sewer Soul Orb) (bow only)
        - beating the Frog King (bow only), the Flooded Fortress bow secret shrine, the Camp of the Free Crows Ice Skating Vitality Shard Shrine and the Castle Lockstone Hookshot Puzzle and Upper Puzzle Levers (their switches need the bow, Fire or Bomb, besides the Hookshot)
        - the Offscreen Targeting Tricks
    - With Start With Bow off, the Reaper's Bow is one of the items Early Important Item can pick.
- **Soul Multiplier**
    - Multiplies the souls you get from enemies and Soul Orbs (1 to 10, default 2).
- **Starting Souls**
    - Souls to start with. Fully upgrading one stat costs 4,300 souls.

### DeathLink Options
These only matter with Deathlink on in the [Mod Options](#mod-options).
- **DeathLink Amnesty**
    - How many of your own deaths are forgiven before one is sent (0 to 20, default 0). With 2, every third death sends a Deathlink.
- **Death Wish**
    - How many received Deathlinks it takes to kill you (1 to 20, default 1). With 3, every third Deathlink you receive kills you; the others just show a counter.
- Both counters are kept in your save file.

## Contributing

If you're interested in helping out, join the discussion in the [Archipelago Discord server](https://discord.com/invite/8Z65BR2). We talk about the development of this in the Death's Door thread in the `custom-games` forum!

### Building the mod

#### Prerequisites
- [.NET Framework 4.7.2 or later](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- [BepInEx 5.4.22 or a later 5.x version](https://github.com/bepinex/bepinex/releases/latest)
  - Do **not** use BepInEx 6.x, as it's in pre-release and may not work properly.
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)

#### Steps
1. Clone this repository.
1. Install the required and recommended mods from [1. Install the mod](#1-install-the-mod) (all but Archipelago Randomizer itself) into your Death's Door plugins folder.
1. Create a new file at the project's root named `config.targets` with the following content:
    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <Project>
      <PropertyGroup>
        <PluginsPath>Path\To\Your\Death's\Door\Plugins\Directory</PluginsPath>
      </PropertyGroup>
    </Project>
    ```
    Replace `Path\To\Your\Death's\Door\Plugins\Directory`. This is usually `[Game directory]\BepInEx\plugins`, unless you changed the default location.
1. In a terminal, go to the project's root folder and run `dotnet restore` to install packages.
1. Build the project.

#### Code Guidelines
- Follow the [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) *except for the following*:
  - **Use 4 space tabs** for indentation.
    - This should be set up for you in the `.editorconfig` file.
  - **Never use implicit typing** (var) for variables.
    - This should be set up for you in the `.editorconfig` file.
- If you add a new mod dependency, do the following:
  - Include it in `Plugin`'s `BepInDependency` attributes.
  - Edit the `.csproj` to add it as a reference, but use dynamic pathing for it using the `PluginsPath` custom MSBuild variable.
    - This ensures the mod is included in the project for anyone who builds it, regardless of difference in paths.
    - Example: `<HintPath>$(PluginsPath)/ItemChanger/ItemChanger.dll</HintPath>`

### Working on the apworld
- The apworld's source is in `APWorld/deaths_door`. To test changes, copy that folder into the `worlds` folder of an [Archipelago source checkout](https://github.com/ArchipelagoMW/Archipelago) (0.6.7 or later) and run Archipelago's tests, or zip it into `deaths_door.apworld` as described in [2. Install Archipelago and the apworld](#2-install-archipelago-and-the-apworld).
- When you change the apworld, raise `world_version` in `APWorld/deaths_door/archipelago.json`.

## Acknowledgements

Thanks to [Chris-is-Awesome](https://github.com/Chris-Is-Awesome) for creating the ArchipelagoRandomizer mod and combining the work of all the amazing people below + maintaining the AP state up to v0.3.3! (Awesome indeed!)

Thanks to [dpinela](https://github.com/dpinela) for their work on the [Multiworld Randomizer](https://github.com/dpinela/DeathsDoor.Randomizer), which the original logic, options and structure of what is randomized come from (many of the glitch descriptions above are taken from their comments), and for some of the dependencies this mod uses: [Alternative Game Modes](https://github.com/dpinela/DeathsDoor.AlternativeGameModes), [Item Changer](https://github.com/dpinela/DeathsDoor.ItemChanger), and [MagicUI](https://github.com/dpinela/DeathsDoor.MagicUI)!

Thanks to [roseasromeo](https://github.com/roseasromeo) for the [Death's Door apworld](https://github.com/roseasromeo/DeathsDoorAPWorld), which now lives in the `APWorld` folder of this repo!

Thanks to DrTChops for [rule_builder](https://github.com/ArchipelagoMW/Archipelago/pull/5048), which the apworld's logic is built on. The apworld would have taken dramatically longer to build without it.

Thanks to [lunix33](https://github.com/lunix33) for starting this project with their work on the first attempt at an [APWorld](https://github.com/lunix33/Archipelago_DeathsDoor/tree/deaths-door/worlds/deaths_door) side of things!

Thanks to [BadMagic](https://github.com/BadMagic100) for their work on [Hollow Knight's Archipelago client](https://github.com/ArchipelagoMW-HollowKnight/Archipelago.HollowKnight), which was used as a reference for the MSBuild properties of this project, and for the original version of MagicUI!

Thanks to [Silent](https://github.com/silent-destroyer) and [Scipio Wright](https://github.com/ScipioWright) for their work on [Tunic's Archipelago client](https://github.com/ScipioWright/tunic-randomizer-archipelago-ER), which was used as a reference for some of the Archipelago integration!
