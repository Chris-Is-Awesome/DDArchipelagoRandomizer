"""Planting pots on the Universal Tracker map.

Each of the 50 pots gets a map pin. The pins are tracker-only event locations (they are not added during
generation): green while the pot's area is reachable and nothing is planted there yet, grey once it has been
planted. The mod writes the save-file ids of planted pots (HealPlant.id) to data storage, and Universal Tracker hands
them to DeathsDoorWorld.reconnect_found_entrances.

Pot ids were read from the game's scene data; each was matched to the pot event (and so the logic region) of the
area it is in by its position on the map.
"""
import dataclasses
from typing import TYPE_CHECKING

from typing_extensions import override

from rule_builder.rules import Rule

from .events import DeathsDoorEventLocationName as EL
from .locations import DeathsDoorLocationName as L

if TYPE_CHECKING:
    from BaseClasses import CollectionState
    from .world import DeathsDoorWorld

PLANTED_POTS_DATASTORAGE_KEY = "{player}_{team}_deathsdoor_planted_pots"
PLANTED_POT_EVENT = "Planted Pot (Tracker)"

# Goal locations that turn into goal events: (map, tracker pin that replaces their pin on that map)
GOAL_PINS: dict[L, tuple[str, str]] = {
    L.RUSTY_BELLTOWER_KEY: ("hall_of_doors", "Beat the Lord of Doors"),
    L.GREEN_ANCIENT_TABLET_OF_KNOWLEDGE: ("estate_of_the_urn_witch", "Open the Green Ancient Tablet Door"),
}
GOAL_PIN_EVENT = "Goal Pin (Tracker)"

# player -> ids of pots planted in, filled by Universal Tracker only
PLANTED_POTS: dict[int, set[str]] = {}


@dataclasses.dataclass(frozen=True)
class Pot:
    pot_id: str  # HealPlant.id in the game's save
    logic_event: EL  # pot event this pot counts for in logic; its region is where the pin is reachable from
    name: str  # name of the tracker pin


POTS: list[Pot] = [
    # Lost Cemetery, Crypt, Stranded Sailor Caves
    Pot("plant_graveyardroute1", EL.POT_OUTSIDE_CATACOMBS_EXIT, "Lost Cemetery - Outside Catacombs Exit Pot"),
    Pot("plant_graveyardcatacombs", EL.POT_CATACOMBS_ROOM_2, "Lost Cemetery - Catacombs Room 2 Pot"),
    Pot("plant_graveyardroute3", EL.POT_CEMETERY_RIGHT_ARENA, "Lost Cemetery - Right Arena Pot"),
    Pot("plant_preservant2", EL.POT_BOMB_SILENT_SERVANT, "Lost Cemetery - Bomb Silent Servant Pot"),
    Pot("plant_graveyardredeemer", EL.POT_CEMETERY_SUMMIT, "Lost Cemetery - Summit Pot"),
    Pot("plant_graveyardroute2", EL.POT_CEMETERY_CENTRAL_ARENA, "Lost Cemetery - Central Arena Pot"),
    Pot("plant_preservant1", EL.POT_FIRE_SILENT_SERVANT, "Crypt - Fire Silent Servant Pot"),
    Pot("plant_preservant3", EL.POT_HOOKSHOT_SILENT_SERVANT, "Stranded Sailor Caves - Hookshot Silent Servant Pot"),
    # Estate of the Urn Witch
    Pot("plant_gardenintro", EL.POT_ESTATE_DOOR, "Estate of the Urn Witch - Door Pot"),
    Pot("plant_gardensouthwest", EL.POT_GARDEN_OF_JOY, "Estate of the Urn Witch - Garden of Joy Pot"),
    Pot("plant_gardensoutheast", EL.POT_GARDEN_OF_PEACE, "Estate of the Urn Witch - Garden of Peace Pot"),
    Pot("plant_gardencentre", EL.POT_GARDEN_OF_LOVE_ENTRANCE, "Estate of the Urn Witch - Garden of Love Entrance Pot"),
    Pot("plant_secret1", EL.POT_GARDEN_OF_LOVE_HIDDEN_LEFT, "Estate of the Urn Witch - Garden of Love Hidden Pot (South)"),
    Pot("plant_secret2", EL.POT_GARDEN_OF_LOVE_HIDDEN_RIGHT, "Estate of the Urn Witch - Garden of Love Hidden Pot (North)"),
    Pot("plant_gardennortheast", EL.POT_GARDEN_OF_LIFE, "Estate of the Urn Witch - Garden of Life Pot"),
    # Ceramic Manor
    Pot("plant_mansioncentre", EL.POT_MANOR_LOBBY, "Ceramic Manor - Lobby Pot"),
    Pot("plant_mansionattic1", EL.POT_MANOR_RAFTERS, "Ceramic Manor - Rafters Pot"),
    Pot("plant_mansionattic2", EL.POT_MANOR_IMP_LOFT, "Ceramic Manor - Imp Loft Pot"),
    Pot("plant_mansiondining", EL.POT_MANOR_POST_DINING, "Ceramic Manor - After Dining Room Pot"),
    Pot("plant_mansionlibrary", EL.POT_MANOR_LIBRARY, "Ceramic Manor - Library Pot"),
    Pot("plant_mansionbedroom", EL.POT_MANOR_BEDROOM, "Ceramic Manor - Bedroom Pot"),
    # Inner Furnace
    Pot("plant_basementplaguewave", EL.POT_INNER_FURNACE_BURNER_2, "Inner Furnace - West Wave Room Pot"),
    Pot("plant_basementwest", EL.POT_INNER_FURNACE_BURNER_3, "Inner Furnace - Conveyor Bridge Pot"),
    Pot("plant_basementeast", EL.POT_INNER_FURNACE_ISLANDS, "Inner Furnace - Islands Pot"),
    Pot("plant_basementcentral", EL.POT_INNER_FURNACE_BURNER_8, "Inner Furnace - Burner 8 Pot"),
    Pot("plant_basementend", EL.POT_INNER_FURNACE_FINAL, "Inner Furnace - Final Pot"),
    # Castle Lockstone, Camp of the Free Crows
    Pot("plant_castlegardenwest", EL.POT_LOCKSTONE_WEST_CROW, "Castle Lockstone - West Crow Pot"),
    Pot("plant_castlenorthwest", EL.POT_LOCKSTONE_WEST_KEYED_CROW, "Castle Lockstone - West Locked Crow Pot"),
    Pot("plant_castleentrance", EL.POT_LOCKSTONE_ENTRANCE, "Castle Lockstone - Entrance Pot"),
    Pot("plant_castlenortheast", EL.POT_LOCKSTONE_EAST_CROW, "Castle Lockstone - East Crow Pot"),
    Pot("plant_fortressexterior", EL.POT_CAMP_CASTLE_DOOR, "Camp of the Free Crows - Castle Door Pot"),
    # Old Watchtowers
    Pot("plant_mountaintopsroute1", EL.POT_WATCHTOWERS_FIRST, "Old Watchtowers - First Pot"),
    Pot("plant_mountaintopsmiddle", EL.POT_WATCHTOWERS_BARB_ELEVATOR, "Old Watchtowers - Barb Elevator Pot"),
    Pot("plant_mountaintopsroute2", EL.POT_WATCHTOWERS_LASERS_ARENA, "Old Watchtowers - Lasers Arena Pot"),
    Pot("plant_mountaintopsroute3", EL.POT_WATCHTOWERS_OWL, "Old Watchtowers - Owl Pot"),
    Pot("plant_mountaintopsend", EL.POT_WATCHTOWERS_CAVE_ENTRANCE, "Old Watchtowers - Cave Entrance Pot"),
    # Overgrown Ruins, Mushroom Dungeon
    Pot("plant_forestintro", EL.POT_RUINS_KEY, "Overgrown Ruins - Key Pot"),
    Pot("plant_forestcorridor", EL.POT_RUINS_FIRE_PLANT_CORRIDOR, "Overgrown Ruins - Fire Plant Corridor Pot"),
    Pot("plant_forestmiddle", EL.POT_RUINS_RIGHT_ARENA, "Overgrown Ruins - Right Arena Pot"),
    Pot("plant_forestlastwave", EL.POT_RUINS_SEWER, "Overgrown Ruins - Sewer Pot"),
    Pot("plant_forestend", EL.POT_RUINS_LEFT_OF_LORD_OF_DOORS_ARENA, "Overgrown Ruins - Left of Lord of Doors Arena Pot"),
    Pot("plant_dungeonnortheast", EL.POT_DUNGEON_COBWEB_ROOM, "Mushroom Dungeon - Cobweb Room Pot"),
    Pot("plant_dungeonconnecting", EL.POT_DUNGEON_WATER_ARENA, "Mushroom Dungeon - Water Arena Pot"),
    Pot("plant_dungeonsouth", EL.POT_DUNGEON_HALL, "Mushroom Dungeon - Hall Pot"),
    Pot("plant_dungeoneast", EL.POT_DUNGEON_RIGHTMOST_CROW, "Mushroom Dungeon - Rightmost Crow Pot"),
    # Flooded Fortress
    Pot("plant_swamproute1", EL.POT_FORTRESS_MAIN_GATE, "Flooded Fortress - Main Gate Pot"),
    Pot("plant_swamproute2", EL.POT_FORTRESS_U_TURN, "Flooded Fortress - U Turn Pot"),
    Pot("plant_swampmiddle", EL.POT_FORTRESS_BREAKABLE_BRIDGES, "Flooded Fortress - Breakable Bridges Pot"),
    Pot("plant_swampwavebattle", EL.POT_FORTRESS_BRIDGE, "Flooded Fortress - Bridge Pot"),
    Pot("plant_swamproute3", EL.POT_FORTRESS_EXIT, "Flooded Fortress - Exit Pot"),
]


@dataclasses.dataclass()
class PotPlanted(Rule["DeathsDoorWorld"], game="Death's Door"):
    """The tracker reports this pot as planted (only used by the tracker-only pot pins)."""

    pot_id: str

    @override
    def _instantiate(self, world: "DeathsDoorWorld") -> "Rule.Resolved":
        return self.Resolved(self.pot_id, player=world.player)

    class Resolved(Rule.Resolved):
        pot_id: str
        skip_cache = True

        @override
        def _evaluate(self, state: "CollectionState") -> bool:
            return self.pot_id in PLANTED_POTS.get(self.player, ())

        @override
        def __str__(self) -> str:
            return f"Planted in {self.pot_id}"
