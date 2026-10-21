"""
Universal Tracker's /get_logical_path for Death's Door: the doors to take to reach a place, in order.

UT's default lists every entrance on the way, including the small steps inside an area, under the entrances'
vanilla names. With entrance randomization those names say where a door led in the vanilla game, not where it
leads in this seed. This lists only the scene transitions, each with where it actually takes you.
"""
from typing import TYPE_CHECKING, Any, Iterator

from BaseClasses import CollectionState, Entrance, Region

if TYPE_CHECKING:
    from .world import DeathsDoorWorld

JSON = list[dict[str, Any]]


def door_label(region_name: str) -> str:
    """'Lost Cemetery Exit to Crypt' -> 'Lost Cemetery: exit to Crypt', 'Door to Lost Cemetery' ->
    'Hall of Doors: Door to Lost Cemetery', 'Lost Cemetery Door' -> 'Lost Cemetery: door to the Hall of Doors'"""
    if region_name.startswith("Door to "):
        return f"Hall of Doors: {region_name}"
    if " Exit to " in region_name:
        area, target = region_name.split(" Exit to ", 1)
        return f"{area}: exit to {target}"
    if region_name.endswith(" Ancient Door"):
        return f"{region_name[: -len(' Ancient Door')]}: Ancient Door"
    if region_name.endswith(" Door"):
        return f"{region_name[: -len(' Door')]}: door to the Hall of Doors"
    return region_name


def is_transition(world: "DeathsDoorWorld", entrance: Entrance) -> bool:
    """A door between scenes (randomized or not), as opposed to a step inside an area"""
    pairings: dict[str, str] = getattr(world, "entrance_pairings", {}) or {}
    return (
        entrance.parent_region is not None
        and entrance.connected_region is not None
        and pairings.get(entrance.parent_region.name) == entrance.connected_region.name
    )


def path_entrances(state: CollectionState, region: Region) -> list[str]:
    """Names of the entrances on the path the state found to a region, from the start"""
    def walk(path_value) -> Iterator[str]:
        while path_value:
            node, path_value = path_value
            yield node

    nodes = list(reversed(list(map(str, walk(state.path.get(region, (region.name, None)))))))
    # Regions and entrances alternate, starting with the start region
    return nodes[1::2]


def logical_path(world: "DeathsDoorWorld", dest_name: str, state: CollectionState) -> list[JSON] | None:
    from Utils import get_intended_text

    player = world.player
    multiworld = world.multiworld
    regions = set(multiworld.regions.region_cache[player])
    locations = set(multiworld.regions.location_cache[player])
    name, usable, _ = get_intended_text(dest_name, regions | locations)
    if not usable:
        return None  # let UT report it

    location = None
    if name in locations:
        location = world.get_location(name)
        if not location.can_reach(state):
            return None
        region = location.parent_region
    else:
        region = world.get_region(name)
        if not region.can_reach(state):
            return None

    messages: list[JSON] = [[
        {"type": "text", "text": "Route to "},
        {"type": "color", "color": "green", "text": door_label(name) if location is None else name},
        {"type": "text", "text": ":"},
    ]]
    step = 0
    for entrance_name in path_entrances(state, region):
        entrance = world.get_entrance(entrance_name)
        if not is_transition(world, entrance):
            continue
        step += 1
        messages.append([
            {"type": "text", "text": f"{step}. Take "},
            {"type": "color", "color": "blue", "text": door_label(entrance.parent_region.name)},
            {"type": "text", "text": "  ->  arrive at "},
            {"type": "color", "color": "yellow", "text": door_label(entrance.connected_region.name)},
        ])
    if step == 0:
        messages.append([{"type": "text", "text": "No doors needed from where you start."}])
    if location is not None:
        messages.append([
            {"type": "text", "text": "Then get "},
            {"type": "color", "color": "green", "text": location.name},
        ])
    return messages
