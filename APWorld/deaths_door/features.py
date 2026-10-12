"""Shop upgrades, planting checks and key-door tracking.

Everything here is kept out of the big name/ID tables so the original data stays untouched.
The mod reads the same names and IDs from its own Items.json / Locations.json.
"""
from typing import TYPE_CHECKING

import dataclasses
from typing_extensions import override

from rule_builder.rules import Rule

from .items import DeathsDoorItemName as I

if TYPE_CHECKING:
    from BaseClasses import CollectionState
    from .world import DeathsDoorWorld

# ---------------------------------------------------------------- shop upgrades
# (display name, game inventory id)
STATS: list[tuple[str, str]] = [
    ("Strength", "stat_melee"),
    ("Dexterity", "stat_dexterity"),
    ("Haste", "stat_haste"),
    ("Magic", "stat_magic"),
]
UPGRADES_PER_STAT = 5
VANILLA_UPGRADE_COSTS = [400, 600, 800, 1000, 1500]  # Globals.upgradeCostPerLevel


def progressive_stat_item(stat: str) -> str:
    return f"Progressive {stat}"


def shop_location(stat: str, level: int) -> str:
    return f"Shop - {stat} Upgrade {level}"


SHOP_ITEM_IDS: dict[str, int] = {progressive_stat_item(stat): 3000 + i for i, (stat, _) in enumerate(STATS)}
SHOP_LOCATION_IDS: dict[str, int] = {
    shop_location(stat, level): 3000 + i * UPGRADES_PER_STAT + (level - 1)
    for i, (stat, _) in enumerate(STATS)
    for level in range(1, UPGRADES_PER_STAT + 1)
}

# Areas with enemies to farm souls in. A shop tier is only in logic once enough of them are reachable, so the shop
# isn't expected before you can earn souls (the Hall of Doors itself has none).
SOUL_AREAS: tuple[str, ...] = (
    "Lost Cemetery Central",
    "Estate of the Urn Witch South",
    "Ceramic Manor Main Lobby",
    "Inner Furnace Entrance",
    "Overgrown Ruins Outside Main Dungeon Gate",
    "Mushroom Dungeon Lobby",
    "Flooded Fortress Frog King Encounter",
    "Stranded Sailor",
    "Castle Lockstone Central",
    "Camp of the Free Crows Village",
    "Old Watchtowers Entrance",
)
# areas needed for the 1st..5th upgrade of a stat
SHOP_TIER_AREAS = [1, 2, 4, 6, 8]


@dataclasses.dataclass()
class CanReachSoulAreas(Rule["DeathsDoorWorld"], game="Death's Door"):
    """Can reach at least `count` of the soul-farming areas."""

    count: int

    @override
    def _instantiate(self, world: "DeathsDoorWorld") -> "Rule.Resolved":
        return self.Resolved(self.count, SOUL_AREAS, player=world.player)

    class Resolved(Rule.Resolved):
        count: int
        regions: tuple[str, ...]
        skip_cache = True

        @override
        def _evaluate(self, state: "CollectionState") -> bool:
            reached = 0
            for region in self.regions:
                if state.can_reach_region(region, self.player):
                    reached += 1
                    if reached >= self.count:
                        return True
            return False

        @override
        def region_dependencies(self) -> dict[str, set[int]]:
            return {region: {id(self)} for region in self.regions}

        @override
        def __str__(self) -> str:
            return f"Can reach {self.count} soul areas"


# ---------------------------------------------------------------- planting
MAX_PLANTED = 50


def planting_location(count: int) -> str:
    return f"Planted {count} Life Seed{'' if count == 1 else 's'}"


PLANTING_LOCATION_IDS: dict[str, int] = {
    planting_location(n): 3100 + n - 1 for n in range(1, MAX_PLANTED + 1)
}

# ---------------------------------------------------------------- key doors
# Save-file ids of the 12 coloured key doors (BaseKey.uniqueId of each lock's "ActualKey"), read from the game's
# scene data. The mod writes the ids of opened doors to data storage; Universal Tracker hands them to
# DeathsDoorWorld.reconnect_found_entrances so the doors behind them stop needing every key of that colour.
# All 12 were matched to their gate in-game (the last door of each colour by elimination).
KEY_DOORS: dict[str, str] = {
    # Pink keys (5)
    "keydoor_covenant": "Camp of the Free Crows - village to elevator",  # only key door in the Camp scene
    "keydoor_graveyardsummit": "Lost Cemetery - summit to belltower",
    "keydoor_graveyard1": "Lost Cemetery - central to Steadhone",
    "ffort_key1": "Castle Lockstone - upper east keyed door",  # last pink door, by elimination
    "ffort_key2": "Castle Lockstone - west keyed crow room",
    # Yellow keys (3)
    "mlock_0": "Ceramic Manor - lobby to library",
    "mlock_1": "Ceramic Manor - lobby to left wing",
    "mlock_elevator": "Ceramic Manor - lobby to Furnace Observation Rooms",  # elevator towards Inner Furnace
    # Green keys (4)
    "fstlock_0": "Overgrown Ruins - main dungeon gate to forest settlement",
    "fstlock_1": "Mushroom Dungeon - main hall to rightmost crow",
    "fstlock_2": "Mushroom Dungeon - main hall to water arena",  # last green door, by elimination
    "fstlock_4": "Mushroom Dungeon - Corrupted Antler",
}

KEY_DOORS_DATASTORAGE_KEY = "{player}_{team}_deathsdoor_opened_key_doors"

# player -> opened door ids. Only ever filled by Universal Tracker; empty during generation, so generation logic
# always needs every key of a colour.
OPENED_KEY_DOORS: dict[int, set[str]] = {}


@dataclasses.dataclass()
class KeyDoor(Rule["DeathsDoorWorld"], game="Death's Door"):
    """Has `count` keys of a colour, or the tracker reports this specific door as already opened."""

    key: I
    count: int
    door_id: str

    @override
    def _instantiate(self, world: "DeathsDoorWorld") -> "Rule.Resolved":
        return self.Resolved(self.key.value, self.count, self.door_id, player=world.player)

    class Resolved(Rule.Resolved):
        key: str
        count: int
        door_id: str
        skip_cache = True

        @override
        def _evaluate(self, state: "CollectionState") -> bool:
            return state.has(self.key, self.player, self.count) or self.door_id in OPENED_KEY_DOORS.get(
                self.player, ()
            )

        @override
        def item_dependencies(self) -> dict[str, set[int]]:
            return {self.key: {id(self)}}

        @override
        def __str__(self) -> str:
            return f"Has {self.count}x {self.key} or opened {self.door_id}"
