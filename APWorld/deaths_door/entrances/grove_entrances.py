from ..rule_builder_overrides import HAS_ANY_SPELL
from .entrance_class import DeathsDoorEntrance
from ..regions import DeathsDoorRegionName as R

grove_entrances: list[DeathsDoorEntrance] = [
    # Entrances in Grove of Spirits
    # From the door, a switch on the way needs any spell (arrow, Fire, Bomb or Hookshot) (Olaf)
    DeathsDoorEntrance(R.GROVE_OF_SPIRITS_DOOR, R.GROVE_OF_SPIRITS, HAS_ANY_SPELL),
    DeathsDoorEntrance(
        R.GROVE_OF_SPIRITS, R.GROVE_OF_SPIRITS_EXIT_TO_LOST_CEMETERY, None
    ),
    DeathsDoorEntrance(
        R.GROVE_OF_SPIRITS_EXIT_TO_LOST_CEMETERY, R.GROVE_OF_SPIRITS, None
    ),
]