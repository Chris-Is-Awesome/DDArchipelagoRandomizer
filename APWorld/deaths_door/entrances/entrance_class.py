from enum import IntEnum
from typing import TYPE_CHECKING, NamedTuple

from rule_builder.rules import Rule

from ..regions import DeathsDoorRegionName as R

if TYPE_CHECKING:
    from .. import DeathsDoorWorld

class DeathsDoorEntrance(NamedTuple):
    starting_region: R
    ending_region: R
    rule: Rule["DeathsDoorWorld"] | None
    name: str = ""
    loading_zone_id: str = ""
    scene_name: str = ""
    no_jefferson: bool = False

class DeathsDoorConnectionGroup(IntEnum):
    STANDARD = 2
    # Two-way transitions that start in the Hall of Doors (its doors to each area). They may not be paired with each
    # other: the mod can't take you from one Hall of Doors door to another (the game froze).
    HALL_OF_DOORS = 3
    ONE_WAY = 1
    JEFFERSON = 20