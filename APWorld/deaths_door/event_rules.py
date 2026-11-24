from .rule_builder_overrides import HAS_BOW, HAS_FIRE_OR_BOW
from .features import KeyDoor
from typing import TYPE_CHECKING
from .rule_builder_overrides import Has, HasAll, HasAny, CanReachRegion
from .items import DeathsDoorItemName as I
from .regions import DeathsDoorRegionName as R
from .options import StartDayOrNight, BombBellGlitch, OffscreenTargetingTricks, Goal, UnrandomizedPools
from .events import (
    DeathsDoorEventLocationName as EL,
    DeathsDoorEventName as E,
    event_location_table,
    pot_table,
)
from .rules import HasEnoughLifeSeeds, HasPlantedEnoughLifeSeeds

from rule_builder.rules import (
    Rule,
    True_,
    OptionFilter,
)


if TYPE_CHECKING:
    from . import DeathsDoorWorld


pot_specific_rules: dict[EL: Rule["DeathsDoorWorld"]] = {
    EL.POT_CATACOMBS_ROOM_2: Has(I.FIRE),
    EL.POT_FIRE_SILENT_SERVANT: Has(I.FIRE),  # behind a spider web in the Crypt
    EL.POT_BOMB_SILENT_SERVANT: Has(I.BOMB),
    EL.POT_MANOR_IMP_LOFT: HAS_FIRE_OR_BOW,
    EL.POT_MANOR_POST_DINING: HAS_FIRE_OR_BOW,
    EL.POT_MANOR_LIBRARY: Has(I.LEVER_MANOR_BOOKSHELF_SHORTCUT) | HAS_FIRE_OR_BOW,
    EL.POT_MANOR_BEDROOM: HasAny(I.HOOKSHOT, I.BOMB) | HAS_FIRE_OR_BOW,
    EL.POT_GARDEN_OF_JOY: HasAny(I.BOMB, I.LEVER_GARDEN_OF_JOY) | HAS_FIRE_OR_BOW,
    EL.POT_GARDEN_OF_LOVE_HIDDEN_LEFT: HAS_BOW,
    EL.POT_GARDEN_OF_LOVE_HIDDEN_RIGHT: HAS_BOW,
    EL.POT_HOOKSHOT_SILENT_SERVANT: Has(I.LEVER_HOOKSHOT_SILENT_SERVANT),
    EL.POT_LOCKSTONE_WEST_KEYED_CROW: KeyDoor(I.PINK_KEY, 5, "ffort_key2"),
    EL.POT_FORTRESS_MAIN_GATE: Has(I.BOMB),
}


deaths_door_event_rules: dict[EL: Rule["DeathsDoorWorld"] | None] = {
    EL.LORD_OF_DOORS: HasAll(E.GREY_CROW_BOSS, I.HOOKSHOT),
    EL.TRUE_ENDING: (HasAll(I.RED_ANCIENT_TABLET_OF_KNOWLEDGE, I.BLUE_ANCIENT_TABLET_OF_KNOWLEDGE, I.CYAN_ANCIENT_TABLET_OF_KNOWLEDGE, I.PURPLE_ANCIENT_TABLET_OF_KNOWLEDGE, I.YELLOW_ANCIENT_TABLET_OF_KNOWLEDGE) & (Has(I.PINK_ANCIENT_TABLET_OF_KNOWLEDGE) | Has(E.PINK_TABLET_PIECE, 3)) & (Has(I.GREEN_ANCIENT_TABLET_OF_KNOWLEDGE) | Has(E.LIFE_SEED_DOOR, options=[OptionFilter(UnrandomizedPools, "Tablet", "contains")]))),
    EL.LIFE_SEED_DOOR: HasPlantedEnoughLifeSeeds() & HAS_BOW,
    # Lighting the lamps at the Estate entrance is the only way on from the exit to the Crypt
    EL.ESTATE_OF_THE_URN_WITCH_ENTRANCE_LAMPS: HAS_FIRE_OR_BOW,
    EL.LOST_CEMETERY_OPENED_EXIT_TO_SAILOR: Has(I.FIRE) | (True_(options=[OptionFilter(OffscreenTargetingTricks, 1)]) & HAS_BOW) | Has(E.OOL),
    EL.FLOODED_FORTRESS_OPENED_BRIDGE: Has(I.LEVER_FORTRESS_NORTH_WEST),
    EL.FLOODED_FORTRESS_OPENED_FORTRESS_ENTRANCE: HAS_BOW,  # shooting the switches in the entrance
    EL.ACCESS_TO_NIGHT: True_(options=[OptionFilter(StartDayOrNight, 1)])
    | (Has(I.RUSTY_BELLTOWER_KEY) & CanReachRegion(R.LOST_CEMETERY_BELLTOWER)) | CanReachRegion(R.LOST_CEMETERY_SUMMIT, options=[OptionFilter(BombBellGlitch, 1)]),
    EL.ACCESS_TO_DAY: True_(options=[OptionFilter(StartDayOrNight, 0)])
    | (Has(I.RUSTY_BELLTOWER_KEY) & CanReachRegion(R.LOST_CEMETERY_BELLTOWER)) | CanReachRegion(R.LOST_CEMETERY_SUMMIT) & (True_(options=[OptionFilter(BombBellGlitch, 1)]) | Has(E.OOL)),
    EL.GREY_CROW_BOSS: HasAll(
        I.GIANT_SOUL_OF_BETTY,
        I.GIANT_SOUL_OF_THE_FROG_KING,
        I.GIANT_SOUL_OF_THE_URN_WITCH,
    ),
    EL.ACTIVATED_FURNACE_BURNERS: Has(I.FIRE),
    EL.WATCHTOWER_ENTRANCE_TORCH: HasAll(
        I.FIRE, E.ACCESS_TO_NIGHT
    ),
    EL.WATCHTOWER_JAMMING_START_TORCH: HasAll(
        I.FIRE, E.ACCESS_TO_NIGHT
    ),
    EL.WATCHTOWER_BOXES_TORCH: HasAll(
        I.FIRE, E.ACCESS_TO_NIGHT
    ),
    EL.WATCHTOWER_FIRST_POT_TORCH: HasAll(
        I.FIRE, E.ACCESS_TO_NIGHT
    ),
    EL.WATCHTOWER_BOOMERS_TORCH: HasAll(
        I.FIRE, E.ACCESS_TO_NIGHT
    ),
    EL.WATCHTOWER_BEFORE_ICE_SKATING_TORCH: HasAll(
        I.FIRE, E.ACCESS_TO_NIGHT
    ),
    EL.MUSHROOM_DUNGEON_MAIN_GATE: HasAll(
        I.MAGICAL_FOREST_HORN, E.ACCESS_TO_DAY
    ),
    EL.RESCUE_GRUNT: Has(I.BOMB),
    EL.CASTLE_LOCKSTONE_LORD_LOCKSTONE: Has(I.FIRE),
    EL.CASTLE_LOCKSTONE_LORD_OPENGATE: Has(I.FIRE),
    EL.CASTLE_LOCKSTONE_LORD_DEADBOLT: Has(I.FIRE),
    EL.CASTLE_LOCKSTONE_LORD_THEODOOR: Has(I.FIRE),
}

# Add in pots to existing tables to be able to use the same infrastructure
for pot in pot_table:
    # Planting in a pot takes one seed; how many seeds you need in total is checked by HasPlantedEnoughLifeSeeds
    # and the planting checks
    pot_rule = Has(I.LIFE_SEED)
    if pot.name in pot_specific_rules.keys():
        pot_rule = pot_rule & pot_specific_rules[pot.name]
    deaths_door_event_rules[pot.name] = pot_rule


def set_event_rules(world: "DeathsDoorWorld") -> None:
    for event_location_data in event_location_table:
        if event_location_data.name in deaths_door_event_rules.keys():
            event_rule = deaths_door_event_rules[event_location_data.name]
        else:
            event_rule = None
        if event_rule is not None:
            event_location = world.get_location(
                event_location_data.name.value
            )
            world.set_rule(event_location, event_rule)
