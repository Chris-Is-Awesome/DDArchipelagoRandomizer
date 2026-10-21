from typing import ClassVar, Any
from logging import warning

from Options import Option, PlandoConnection

from rule_builder.rules import Rule, False_
from rule_builder.cached_world import CachedRuleBuilderWorld
from .options import (
    DeathsDoorOptions,
    Goal,
    TrapTypeWeights,
    deathsdoor_options_presets,
    deathsdoor_option_groups,
    StartWeapon,
    EarlyImportantItem,
    ShopPrices,
    EntranceRandomization,
)
from .vanilla_pools import vanilla_location_lookup
from .items import (
    item_name_to_id,
    item_table,
    item_name_groups,
    ItemGroup as IG,
    DeathsDoorItemName as I,
)
from .locations import (
    DeathsDoorLocationName as L,
    location_name_to_id,
    location_table,
    location_name_groups,
)
from .events import (
    DeathsDoorEventName as E,
    event_location_table,
)
from .event_rules import set_event_rules, pot_specific_rules
from .regions import DeathsDoorRegionName as R
from .entrances.entrances import deathsdoor_internal_entrances
from .entrances.randomize_transitions import (
    connect_entrances_function,
    create_vanilla_entrances,
    connect_plando_connection,
)
from .features import (
    STATS,
    UPGRADES_PER_STAT,
    VANILLA_UPGRADE_COSTS,
    MAX_PLANTED,
    SHOP_ITEM_IDS,
    SHOP_LOCATION_IDS,
    PLANTING_LOCATION_IDS,
    KEY_DOORS_DATASTORAGE_KEY,
    OPENED_KEY_DOORS,
    progressive_stat_item,
    shop_location,
    planting_location,
    CanReachSoulAreas,
    SHOP_TIER_AREAS,
)
from .jefferson import (
    create_jefferson_regions,
    create_jefferson_internal_connections,
)
from .rules import Has, set_location_rules, deaths_door_location_rules
from worlds.AutoWorld import World, WebWorld
from BaseClasses import MultiWorld, Region, Location, Item, ItemClassification, Tutorial, CollectionState

from .tracker import tracker_world
from .route import logical_path
from .pots import POTS, PLANTED_POTS, PLANTED_POTS_DATASTORAGE_KEY, PLANTED_POT_EVENT, PotPlanted, GOAL_PINS, GOAL_PIN_EVENT
from .json_generator import (
    generate_rule_json,
    generate_items_json,
    generate_locations_json,
    generate_scene_transition_json,
)

deathsdoor_version = "0.4.2"

# Below this many randomized locations, an unrandomized Soul Orb pool is randomized anyway (see generate_early)
MIN_LOCATIONS_WITHOUT_SOUL_ORBS = 70


class DeathsDoorItem(Item):
    game: str = "Death's Door"


class DeathsDoorLocation(Location):
    game: str = "Death's Door"


class DeathsDoorWeb(WebWorld):
    theme = "stone"
    game = "Death's Door"
    option_groups = deathsdoor_option_groups
    options_presets = deathsdoor_options_presets

    tutorials = [
        Tutorial(
            tutorial_name="Multiworld Setup Guide",
            description="A guide to setting up the Death's Door Randomizer for Archipelago multiworld games.",
            language="English",
            file_name="setup_en.md",
            link="setup/en",
            authors=[""],
        )
    ]


class DeathsDoorWorld(CachedRuleBuilderWorld, World):
    """Reaping souls of the dead and punching a clock might get monotonous but it's honest work for a Crow.
    The job gets lively when your assigned soul is stolen and you must track down a desperate thief to a realm
    untouched by death - where creatures grow far past their expiry."""

    game = "Death's Door"  # name of the game/world
    web = DeathsDoorWeb()
    options_dataclass = DeathsDoorOptions  # options the player can set
    options: DeathsDoorOptions  # typing hints for option results
    topology_present = True  # show path to required location checks in spoiler
    origin_region_name = R.HALL_OF_DOORS_LOBBY

    item_name_to_id = {**item_name_to_id, **SHOP_ITEM_IDS}
    location_name_to_id = {**location_name_to_id, **SHOP_LOCATION_IDS, **PLANTING_LOCATION_IDS}
    item_name_groups = {**item_name_groups, "Stat Upgrades": set(SHOP_ITEM_IDS)}
    location_name_groups = {
        **location_name_groups,
        "Shop": set(SHOP_LOCATION_IDS),
        "Planting": set(PLANTING_LOCATION_IDS),
    }

    #  UT Integration
    tracker_world: ClassVar[dict[str, Any]] = tracker_world
    ut_can_gen_without_yaml: ClassVar[bool] = True
    glitches_item_name: ClassVar[str] = E.OOL.value
    found_entrances_datastorage_key = [
        "{player}_{team}_deathsdoor_found_entrances",
        KEY_DOORS_DATASTORAGE_KEY,
        PLANTED_POTS_DATASTORAGE_KEY,
    ]

    # rule_builder
    rule_caching_enabled = False

    @staticmethod
    def interpret_slot_data(slot_data: dict[str, Any]) -> dict[str, Any]:

        # Adapted from ClassicSpeed's SADX implementation of this feature
        # Patch releases (0.4.x) generate identical seeds, so only major.minor has to match
        def release(version: str) -> list[str]:
            return str(version).split(".")[:2]

        if (
            "APWorldVersion" in slot_data
            and release(slot_data["APWorldVersion"]) != release(deathsdoor_version)
        ):
            current_version = f"{deathsdoor_version}"
            slot_version = f"v{slot_data['APWorldVersion']}"

            raise Exception(
                f"Death's Door version error: The version of apworld used to generate this world ({slot_version}) "
                f"does not match the version of your installed apworld ({current_version})."
            )
        return slot_data

    def is_tracker(self) -> bool:
        """Universal Tracker rebuilding the world from slot data"""
        return bool(getattr(self.multiworld, "re_gen_passthrough", {}))

    def generate_early(self) -> None:
        # Add an important item to early or local_early if option is set
        early_important_item_candidates: list[I] = [
            I.CERAMIC_MANOR_DOOR,
            I.INNER_FURNACE_DOOR,
            I.LOST_CEMETERY_DOOR,
            I.GROVE_OF_SPIRITS_DOOR,
            I.OLD_WATCHTOWERS_DOOR,
            I.OVERGROWN_RUINS_DOOR,
            I.STRANDED_SAILOR_DOOR,
            I.CASTLE_LOCKSTONE_DOOR,
            I.FLOODED_FORTRESS_DOOR,
            I.MUSHROOM_DUNGEON_DOOR,
            I.CAMP_OF_THE_FREE_CROWS_DOOR,
            I.ESTATE_OF_THE_URN_WITCH_DOOR,
            I.HOOKSHOT,
            I.BOMB,
            I.FIRE,
        ]
        important_item = self.random.choice(early_important_item_candidates)
        if self.options.early_important_item == EarlyImportantItem.option_early:
            self.multiworld.early_items[self.player][important_item.value] = 1
        elif self.options.early_important_item == EarlyImportantItem.option_local_early:
            self.multiworld.local_early_items[self.player][important_item.value] = 1

        # Soul Orbs are the only pure filler pool. Without them every item has to fit exactly into the few
        # randomized locations, which fails generation far too often, so randomize them when the pool is that small.
        if "Soul Orb" in self.options.unrandomized_pools.value and not getattr(self.multiworld, "re_gen_passthrough", {}):
            unrandomized = self.options.unrandomized_pools.value
            randomized_locations = sum(
                1
                for location_data in location_table
                if not unrandomized.intersection(group.value for group in location_data.location_groups)
            )
            if randomized_locations < MIN_LOCATIONS_WITHOUT_SOUL_ORBS:
                warning(
                    f"{self.player_name}: Only {randomized_locations} locations are randomized with these "
                    f"unrandomized_pools, which is too few to generate reliably. Soul Orbs will be randomized."
                )
                self.options.unrandomized_pools.value = unrandomized - {"Soul Orb"}

        # warn for all the incompatible options
        if "Weapon" in self.options.unrandomized_pools.value:
            if self.options.start_weapon != StartWeapon.option_sword:
                warning(
                    f"{self.player_name}: If Weapons are not randomized, start weapon will be forced to be sword."
                )
                self.options.start_weapon.value = StartWeapon.option_sword
        if "Shiny Thing" in self.options.unrandomized_pools.value:
            warning(
                f"{self.player_name}: If Shiny Things are not randomized, Rusty Belltower Key will still be added to "
                "the pool so that the player has access to day/night"
            )
            if "Soul Orb" in self.options.unrandomized_pools.value:
                warning(
                    f"{self.player_name}: Without Soul Orbs and Shiny Things, there is no location to put the Rusty "
                    "Belltower Key, so a random item will be added to the player's start inventory"
                )
        if "Soul Orb" in self.options.unrandomized_pools.value:
            if (
                self.options.extra_life_seeds.value > 0
                or self.options.extra_magic_shards.value > 0
                or self.options.extra_vitality_shards > 0
            ):
                warning(
                    f"{self.player_name}: If Soul Orbs are not randomized, no extra items can be safely added to the "
                    "seed. Extra Life Seeds, Extra Magic Shards, and Extra Vitality Shards will be set to 0."
                )
                self.options.extra_life_seeds.value = 0
                self.options.extra_magic_shards.value = 0
                self.options.extra_vitality_shards.value = 0

        if self.options.start_weapon == StartWeapon.option_random_excluding_umbrella:
            self.options.start_weapon.value = self.random.randint(0, 3)

        if self.options.extra_life_seeds < 0:
            max_life_seeds_to_remove = 50 - self.options.plant_pot_number.value
            if max_life_seeds_to_remove < -self.options.extra_life_seeds.value:
                warning(
                    f"{self.options.extra_life_seeds} is too many Life Seeds to remove based on a plant_pot_number of {self.options.plant_pot_number}. Only {max_life_seeds_to_remove} will be removed instead."
                )
                self.options.extra_life_seeds.value = -max_life_seeds_to_remove

        # Universal Tracker slot data handling
        re_gen_passthrough = getattr(self.multiworld, "re_gen_passthrough", {})
        if re_gen_passthrough and self.game in re_gen_passthrough:
            # Get the passed through slot data from the real generation
            slot_data: dict[str, Any] = re_gen_passthrough[self.game]

            # Set all your options here instead of getting them from the yaml
            for key, value in slot_data.items():
                opt: Option[Any] | None = getattr(self.options, key, None)
                if opt is not None:
                    # You can also set .value directly but that won't work if you have OptionSets
                    setattr(self.options, key, opt.from_any(value))
            self.options.plando_connections.value.clear()
            if "entrance_pairings" in slot_data.keys():
                entrance_pairings: dict[str, str] = slot_data["entrance_pairings"]
                for entrance, exit in entrance_pairings.items():
                    self.options.plando_connections.value.append(
                        PlandoConnection(entrance, exit, "entrance")
                    )

        self.setup_shop_and_planting()

    def setup_shop_and_planting(self) -> None:
        re_gen_passthrough = getattr(self.multiworld, "re_gen_passthrough", {})
        slot_data: dict[str, Any] = re_gen_passthrough.get(self.game, {}) if re_gen_passthrough else {}

        # Shop prices: stat id -> price of the 1st..5th purchase
        self.shop_price_table: dict[str, list[int]] = {}
        if self.options.shop_upgrades:
            if "shop_price_table" in slot_data:
                self.shop_price_table = {k: list(v) for k, v in slot_data["shop_price_table"].items()}
            elif self.options.shop_prices == ShopPrices.option_shuffled:
                prices = VANILLA_UPGRADE_COSTS * len(STATS)
                self.random.shuffle(prices)
                for i, (_, stat_id) in enumerate(STATS):
                    # each stat keeps rising prices, like the vanilla shop
                    self.shop_price_table[stat_id] = sorted(prices[i * UPGRADES_PER_STAT:(i + 1) * UPGRADES_PER_STAT])
            elif self.options.shop_prices == ShopPrices.option_random_range:
                low, high = sorted((self.options.shop_price_minimum.value, self.options.shop_price_maximum.value))
                for _, stat_id in STATS:
                    self.shop_price_table[stat_id] = sorted(
                        self.random.randint(low, high) for _ in range(UPGRADES_PER_STAT)
                    )
            else:
                for _, stat_id in STATS:
                    self.shop_price_table[stat_id] = list(VANILLA_UPGRADE_COSTS)

        # Planting checks: never ask for more seeds than exist in the pool or pots in the world
        per_check = self.options.seeds_per_planting_check.value
        if "Life Seed" in self.options.unrandomized_pools.value:
            seeds_available = MAX_PLANTED
        else:
            seeds_available = MAX_PLANTED + self.options.extra_life_seeds.value
        max_checks = min(MAX_PLANTED, seeds_available) // per_check
        if self.options.planting_checks.value > max_checks:
            warning(
                f"{self.player_name}: Only {max_checks} planting checks of {per_check} seeds fit the "
                f"{min(MAX_PLANTED, seeds_available)} Life Seeds available, lowering planting_checks to {max_checks}."
            )
            self.options.planting_checks.value = max_checks
        self.planting_thresholds: list[int] = [
            per_check * k for k in range(1, self.options.planting_checks.value + 1)
        ]

    def create_regions(self) -> None:
        # goal location -> its region, for the goal locations that became goal events
        self.goal_event_locations: dict[L, Region] = {}
        for deathsdoor_region in R:
            region = Region(deathsdoor_region.value, self.player, self.multiworld)
            self.multiworld.regions.append(region)
        create_jefferson_regions(self)

        for location_data in location_table:
            region = self.get_region(location_data.region.value)
            current_location_group_names = set(
                location_group.value for location_group in location_data.location_groups
            )

            # Goal locations are replaced by the goal event regardless of which pools are randomized; otherwise an
            # unrandomized Shiny Thing / Tablet pool would put the vanilla item there and the goal could never be met.
            if (
                self.options.goal == Goal.option_lord_of_doors
                or self.options.goal == Goal.option_any
            ) and location_data.name == L.RUSTY_BELLTOWER_KEY:
                location = DeathsDoorLocation(self.player, location_data.name.value, None, region)
                region.locations.append(location)
                location.place_locked_item(self.create_event(E.LORD_OF_DOORS.value))
                self.goal_event_locations[location_data.name] = region
                continue
            if (
                self.options.goal == Goal.option_green_tablet
                or self.options.goal == Goal.option_any
            ) and location_data.name == L.GREEN_ANCIENT_TABLET_OF_KNOWLEDGE:
                location = DeathsDoorLocation(self.player, location_data.name.value, None, region)
                region.locations.append(location)
                location.place_locked_item(self.create_event(E.LIFE_SEED_DOOR.value))
                self.goal_event_locations[location_data.name] = region
                continue

            # Make all locations that have not been specifically chosen in unrandomized_pools
            # If we somehow end up with items in two location groups, only omit them from randomization if all of their groups have been selected for unrandomized
            if (
                len(
                    self.options.unrandomized_pools.value.intersection(
                        current_location_group_names
                    )
                )
                == 0
            ):
                # Skip locations that are irrelevant on specific goals
                if (
                    self.options.goal == Goal.option_lord_of_doors
                    or self.options.goal == Goal.option_any
                ) and location_data.name == L.RUSTY_BELLTOWER_KEY:
                    location = DeathsDoorLocation(
                        self.player,
                        location_data.name.value,
                        None,
                        region,
                    )
                    region.locations.append(location)
                    location.place_locked_item(self.create_event(E.LORD_OF_DOORS.value))
                elif (
                    self.options.goal == Goal.option_green_tablet
                    or self.options.goal == Goal.option_any
                ) and location_data.name == L.GREEN_ANCIENT_TABLET_OF_KNOWLEDGE:
                    location = DeathsDoorLocation(
                        self.player,
                        location_data.name.value,
                        None,
                        region,
                    )
                    region.locations.append(location)
                    location.place_locked_item(
                        self.create_event(E.LIFE_SEED_DOOR.value)
                    )
                else:
                    location = DeathsDoorLocation(
                        self.player,
                        location_data.name.value,
                        location_data.location_id,
                        region,
                    )
                    region.locations.append(location)
            elif (
                "Life Seed" in self.options.unrandomized_pools.value
                and "Life Seed" in current_location_group_names
            ):
                # To avoid having to list all the life seeds individually
                location = DeathsDoorLocation(
                    self.player,
                    location_data.name.value,
                    None,
                    region,
                )
                region.locations.append(location)
                location.place_locked_item(self.create_event(I.LIFE_SEED.value))
            else:
                # Create an event with the vanilla item name as needed
                if location_data.name in vanilla_location_lookup.keys():
                    location = DeathsDoorLocation(
                        self.player,
                        location_data.name.value,
                        None,
                        region,
                    )
                    region.locations.append(location)
                    location.place_locked_item(
                        self.create_event(
                            vanilla_location_lookup[location_data.name].value
                        )
                    )
            # Skip all other pools/locations selected as unrandomized (ex. non-progression)

        for event_location_data in event_location_table:
            region = self.get_region(event_location_data.region.value)
            event_location = DeathsDoorLocation(
                self.player, event_location_data.name.value, None, region
            )
            region.locations.append(event_location)
            event_location.place_locked_item(
                self.create_event(event_location_data.event_name.value)
            )

        if self.is_tracker():
            # Map pins for the planting pots, only in Universal Tracker (see pots.py)
            # Each pin sits in its own small region behind the pot's own requirement (fire for spider webs, keys,
            # ...), so the pin is only green once you can actually get to the pot.
            pot_regions = {data.name: data.region for data in event_location_table}
            for pot in POTS:
                region = Region(f"{pot.name} (Tracker)", self.player, self.multiworld)
                self.multiworld.regions.append(region)
                self.get_region(pot_regions[pot.logic_event].value).connect(region, f"To {pot.name}")
                pin = DeathsDoorLocation(self.player, pot.name, None, region)
                region.locations.append(pin)
                pin.place_locked_item(self.create_event(PLANTED_POT_EVENT))

        if self.is_tracker():
            # A goal event in a reachable area shows as "passable" (green) on the tracker map even while the goal
            # can't be done yet. Hide that pin and show one that only turns green once the goal's requirements are met.
            hidden: dict[str, list[str]] = {}
            for goal_location, region in self.goal_event_locations.items():
                map_name, pin_name = GOAL_PINS[goal_location]
                hidden.setdefault(map_name, []).append(goal_location.value)
                pin_region = Region(f"{pin_name} (Tracker)", self.player, self.multiworld)
                self.multiworld.regions.append(pin_region)
                region.connect(pin_region, f"To {pin_name}")
                pin = DeathsDoorLocation(self.player, pin_name, None, pin_region)
                pin_region.locations.append(pin)
                pin.place_locked_item(self.create_event(GOAL_PIN_EVENT))
            self.ut_map_page_hidden_events = hidden

        # Shop and planting checks live in the Hall of Doors, which is always reachable
        hall = self.get_region(R.HALL_OF_DOORS_LOBBY.value)
        if self.options.shop_upgrades:
            for stat, _ in STATS:
                for level in range(1, UPGRADES_PER_STAT + 1):
                    name = shop_location(stat, level)
                    hall.locations.append(DeathsDoorLocation(self.player, name, SHOP_LOCATION_IDS[name], hall))
        for count in self.planting_thresholds:
            name = planting_location(count)
            hall.locations.append(DeathsDoorLocation(self.player, name, PLANTING_LOCATION_IDS[name], hall))

        for deathsdoor_entrance in deathsdoor_internal_entrances:
            start_region = self.multiworld.get_region(
                deathsdoor_entrance.starting_region.value, self.player
            )
            end_region = self.multiworld.get_region(
                deathsdoor_entrance.ending_region.value, self.player
            )
            self.create_entrance(start_region, end_region, deathsdoor_entrance.rule)
        create_jefferson_internal_connections(self)
        create_vanilla_entrances(self)

    def create_item(self, name: str, useful: bool = False) -> DeathsDoorItem:
        # if the name provided is an event, create it as an event
        if name in E:
            return DeathsDoorItem(
                name, ItemClassification.progression, None, self.player
            )

        if name in SHOP_ITEM_IDS:
            return DeathsDoorItem(name, ItemClassification.useful, SHOP_ITEM_IDS[name], self.player)

        # otherwise, look up the item data
        item_data = next(data for data in item_table if data.name.value == name)
        if useful:
            # Used to create useful versions instead of progression based on option
            # Standard create_item will still create the progression version
            return DeathsDoorItem(
                name, ItemClassification.useful, self.item_name_to_id[name], self.player
            )
        elif (
            self.options.goal == Goal.option_lord_of_doors
            or self.options.goal == Goal.option_green_tablet
        ) and IG.TABLET in item_data.item_groups:
            return DeathsDoorItem(
                name, ItemClassification.filler, self.item_name_to_id[name], self.player
            )
        else:
            return DeathsDoorItem(
                name, item_data.classification, self.item_name_to_id[name], self.player
            )

    def create_event(self, name: str) -> DeathsDoorItem:
        # for creating event versions of items for non-randomized pools
        return DeathsDoorItem(name, ItemClassification.progression, None, self.player)
    
    def create_items(self) -> None:
        def choose_trap(trap_weights: TrapTypeWeights) -> str:
            trap_weights_sum = sum(trap_weights.values())
            return self.random.choices(list(k for k in trap_weights.keys()), list((w/trap_weights_sum) for w in trap_weights.values()))[0]

        deathsdoor_items: list[DeathsDoorItem] = []
        items_to_create: dict[str, int] = {data.name.value: 0 for data in item_table if data.classification == ItemClassification.trap}

        trap_weights = self.options.trap_type_weights
        trap_chance = self.options.trap_chance / 100
        are_traps_added = trap_chance > 0 and any(v > 0 for v in trap_weights.values())

        for data in item_table:
            if len(
                self.options.unrandomized_pools.value.intersection(
                    set(data.item_groups)
                )
            ) == 0:  # filter out unrandomized pools
                if are_traps_added and data.classification == ItemClassification.filler:
                    trap_number = self.random.binomialvariate(data.base_quantity_in_item_pool, trap_chance)
                    items_to_create[data.name.value] = data.base_quantity_in_item_pool - trap_number
                    for _ in range(0, trap_number):
                        trap = choose_trap(trap_weights)
                        items_to_create[trap] += 1
                else:
                    items_to_create[data.name.value] = data.base_quantity_in_item_pool

        if (
            "Weapon" not in self.options.unrandomized_pools.value
            and self.options.remove_spell_upgrades.value
        ):
            items_to_create[I.FIRE] = 1
            items_to_create[I.BOMB] = 1
            items_to_create[I.HOOKSHOT] = 1
            items_to_create[I.BOW] = 0

        if self.options.start_weapon != StartWeapon.option_sword:
            # If Weapon unrandomized, start_weapon forced to be option_sword
            # Default is that Reaper's Sword is not in the pool, and the others are
            # Mod handles granting the starting weapon
            items_to_create[I.SWORD.value] = 1
            if self.options.start_weapon == StartWeapon.option_daggers:
                items_to_create[I.ROGUE_DAGGERS.value] = 0
            elif self.options.start_weapon == StartWeapon.option_hammer:
                items_to_create[I.THUNDER_HAMMER.value] = 0
            elif self.options.start_weapon == StartWeapon.option_greatsword:
                items_to_create[I.REAPERS_GREATSWORD.value] = 0
            elif self.options.start_weapon == StartWeapon.option_umbrella:
                items_to_create[I.DISCARDED_UMBRELLA.value] = 0

        if not self.options.roll_buffers:
            # If roll_buffers is not on, convert the weapons back to being useful
            # Done so to ensure that any weapons used in rules are created by create_item as progression if we aren't directly responsible for creating it
            for weapon_name in [
                I.SWORD.value,
                I.ROGUE_DAGGERS.value,
                I.REAPERS_GREATSWORD,
                I.DISCARDED_UMBRELLA,
            ]:
                if (
                    weapon_name in items_to_create.keys()
                    and items_to_create[weapon_name] == 1
                ):
                    items_to_create[weapon_name] = 0
                    deathsdoor_items.append(self.create_item(weapon_name, True))

        if "Life Seed" not in self.options.unrandomized_pools.value:
            # Every Life Seed is progression: in game any seed can be planted, so logic (and the tracker) has to
            # count all of them, not just the first N. extra_life_seeds adds or removes seeds.
            items_to_create[I.LIFE_SEED.value] = 50 + self.options.extra_life_seeds.value

        # Create extra magic shards
        for _ in range(self.options.extra_magic_shards.value):
            deathsdoor_items.append(self.create_item(I.MAGIC_SHARD.value))

        # Create extra vitality shards
        for _ in range(self.options.extra_vitality_shards.value):
            deathsdoor_items.append(self.create_item(I.VITALITY_SHARD.value))

        for item, quantity in items_to_create.items():
            for _ in range(quantity):
                deathsdoor_items.append(self.create_item(item))

        if "Shiny Thing" in self.options.unrandomized_pools.value:
            # Still add the Rusty Belltower Key to pool so it is accessible for night
            deathsdoor_items.append(self.create_item(I.RUSTY_BELLTOWER_KEY.value))
            if "Soul Orb" in self.options.unrandomized_pools.value:
                # randomly choose an item to precollect, since there won't be space
                # for the Belltower key
                removed_item: DeathsDoorItem = self.random.choice(deathsdoor_items)
                self.push_precollected(removed_item)
                deathsdoor_items.remove(removed_item)

        if self.options.shop_upgrades:
            for stat, _ in STATS:
                for _ in range(UPGRADES_PER_STAT):
                    deathsdoor_items.append(self.create_item(progressive_stat_item(stat)))
        for _ in range(self.options.extra_stat_upgrades.value):
            stat, _ = self.random.choice(STATS)
            deathsdoor_items.append(self.create_item(progressive_stat_item(stat)))

        if "Door" in self.options.unrandomized_pools.value:
            # With vanilla doors, Castle Lockstone and the Camp of the Free Crows are only reached through the Camp
            # elevator's pink key door, and logic has to assume every pink key may be spent elsewhere first. With so
            # few randomized locations left, fill failed in about 1 in 7 such seeds when fuzzing, so start with those two doors.
            for door in (I.CASTLE_LOCKSTONE_DOOR.value, I.CAMP_OF_THE_FREE_CROWS_DOOR.value):
                self.push_precollected(self.create_item(door))

        junk = len(self.multiworld.get_unfilled_locations(self.player)) - len(
            deathsdoor_items
        )
        if junk < 0:
            # More items than open locations (e.g. the goal removed a location while Soul Orbs, the only pure
            # filler pool, are unrandomized). Drop filler first, then start the player with the least important items.
            warning(
                f"{self.player_name}: {-junk} more item(s) than locations with these options; "
                "removing filler / adding the least important items to the starting inventory."
            )
            priority = {
                ItemClassification.filler: 0,
                ItemClassification.trap: 1,
                ItemClassification.useful: 2,
            }
            ordered = sorted(
                deathsdoor_items,
                key=lambda item: (
                    priority.get(item.classification, 3),
                    self.random.random(),
                ),
            )
            for item in ordered[:-junk]:
                deathsdoor_items.remove(item)
                if item.classification not in (ItemClassification.filler, ItemClassification.trap):
                    self.push_precollected(item)
            junk = 0
        trap_number = self.random.binomialvariate(junk, trap_chance)
        deathsdoor_items += [
            self.create_item(self.get_filler_item_name()) for _ in range(junk - trap_number)
        ]
        deathsdoor_items += [
            self.create_item(choose_trap(trap_weights)) for _ in range(trap_number)
        ]

        self.multiworld.itempool += deathsdoor_items

    def set_rules(self) -> None:
        set_location_rules(self)
        set_event_rules(self)
        if self.options.shop_upgrades:
            for stat, _ in STATS:
                for level in range(1, UPGRADES_PER_STAT + 1):
                    self.set_rule(
                        self.get_location(shop_location(stat, level)),
                        CanReachSoulAreas(SHOP_TIER_AREAS[level - 1]),
                    )
        if self.is_tracker():
            for goal_location in self.goal_event_locations:
                _, pin_name = GOAL_PINS[goal_location]
                goal_rule = deaths_door_location_rules.get(goal_location)
                if goal_rule is not None:
                    self.set_rule(self.get_entrance(f"To {pin_name}"), goal_rule)
                # never "done": the pin stays green once the goal can be completed
                self.set_rule(self.get_location(pin_name), False_())
            for pot in POTS:
                if pot.logic_event in pot_specific_rules:
                    self.set_rule(self.get_entrance(f"To {pot.name}"), pot_specific_rules[pot.logic_event])
                self.set_rule(self.get_location(pot.name), PotPlanted(pot.pot_id))
        for count in self.planting_thresholds:
            # Planting N seeds needs N seeds and N pots you can reach (each pot event needs one seed)
            self.set_rule(
                self.get_location(planting_location(count)),
                Has(I.LIFE_SEED, count) & Has(E.PLANTED_SEED, count),
            )

        completion_rule: Rule = False_()
        if (
            self.options.goal == Goal.option_lord_of_doors
            or self.options.goal == Goal.option_any
        ):
            completion_rule = completion_rule | Has(E.LORD_OF_DOORS)
        if (
            self.options.goal == Goal.option_true_ending
            or self.options.goal == Goal.option_any
        ):
            completion_rule = completion_rule | Has(E.TRUE_ENDING)
        if (
            self.options.goal == Goal.option_green_tablet
            or self.options.goal == Goal.option_any
        ):
            completion_rule = completion_rule | Has(E.LIFE_SEED_DOOR)

        self.set_completion_rule(completion_rule)
        self.register_rule_builder_dependencies()

        # generate_rule_json()
        # generate_items_json()
        # generate_locations_json()
        # generate_scene_transition_json()

    def connect_entrances(self) -> None:
        self.entrance_pairings: dict[str, str] = {}
        connect_entrances_function(self)
    
    def get_logical_path(self, dest_name: str, state: CollectionState) -> list | None:
        """Universal Tracker's /get_logical_path: the doors to take, in order, with where each one leads"""
        return logical_path(self, dest_name, state)

    def reconnect_found_entrances(self, found_key: str, data_storage_value) -> None:
        if found_key.endswith("_deathsdoor_planted_pots"):
            # Pots the player planted in turn grey on the tracker map
            PLANTED_POTS[self.player] = set(data_storage_value or [])
            return
        if found_key.endswith("_deathsdoor_opened_key_doors"):
            # Key doors the player already opened no longer need every key of that colour (tracker only)
            OPENED_KEY_DOORS[self.player] = set(data_storage_value or [])
            return
        coupled = (
            self.options.entrance_randomization == EntranceRandomization.option_coupled
        )
        if data_storage_value != None:
            for entrance in data_storage_value:
                for plando_connection in self.options.plando_connections:
                    if plando_connection.entrance == entrance:
                        connect_plando_connection(self, plando_connection, coupled)
                        break
        # from Utils import visualize_regions
        # from BaseClasses import CollectionState
        # start_region = self.get_region(self.origin_region_name)
        # visualize_regions(
        #     start_region,
        #     "DD_visualization.puml",
        #     regions_to_highlight=CollectionState(
        #         self.multiworld
        #     ).reachable_regions[self.player],
        # )

    def fill_slot_data(self) -> dict[str, Any]:
        # A dictionary returned from this method gets set as the slot_data and will be sent to the client after connecting.
        # The options dataclass has a method to return a `Dict[str, Any]` of each option name provided and the relevant
        # option's value.
        slot_data = self.options.as_dict(
            "start_day_or_night",
            "start_weapon",
            "plant_pot_number",
            "soul_multiplier",
            "starting_souls",
            "unrandomized_pools",
            "goal",
            "gate_rolls_glitch",
            "bomb_bell_glitch",
            "offscreen_targeting_tricks",
            "geometry_exploits",
            "roll_buffers",
            "entrance_randomization",
            toggles_as_bools=True,
        )
        slot_data["entrance_pairings"] = self.entrance_pairings
        slot_data["shop_upgrades"] = bool(self.options.shop_upgrades)
        slot_data["shop_price_table"] = self.shop_price_table
        slot_data["shop_hints"] = bool(self.options.shop_hints)
        slot_data["planting_checks"] = self.options.planting_checks.value
        slot_data["seeds_per_planting_check"] = self.options.seeds_per_planting_check.value
        slot_data["planting_thresholds"] = self.planting_thresholds
        slot_data["APWorldVersion"] = deathsdoor_version
        return slot_data

    def get_filler_item_name(self) -> str:
        return I.SOULS.value

    @classmethod
    def stage_fill_hook(
        cls,
        multiworld: MultiWorld,
        progitempool: list[Item],
        usefulitempool: list[Item],
        filleritempool: list[Item],
        fill_locations: list[Location],
    ) -> None:
        # This function was borrowed from Mysteryem's implementation of Lego Star Wars Complete Saga
        # https://github.com/Mysteryem/Archipelago-TCS/blob/v1.0.1/lego_star_wars_tcs/__init__.py#L1298-L1334
        game_player_ids = set(multiworld.get_game_players(cls.game))
        game_minimal_player_ids = {
            player
            for player in game_player_ids
            if multiworld.worlds[player].options.accessibility == "minimal"
        }

        def sort_func(item: Item):
            if (
                item.player in game_player_ids
                and item.name == I.LIFE_SEED.value
                and ItemClassification.progression in item.classification
            ):
                # Only sort the progression Life Seeds
                if item.player in game_minimal_player_ids:
                    # For minimal players, place Life Seeds first. This helps prevent fill from dumping logically relevant
                    # items into unreachable locations and reducing the number of reachable locations to fewer than the
                    # number of items remaining to be placed.
                    #
                    # Forcing Life Seeds first has the unfortunately sideeffect of priority fill picking Life Seeds first,
                    # but that will just have to be put up with in order to generate well. Maybe a small buffer of
                    # non-Life Seed items could be placed first so that the items in the buffer end up on priority
                    # locations.
                    return 1
                else:
                    # For non-minimal players, place Life Seeds last. This strategy helps prevent fill from filling most/all
                    # reachable locations with the macguffins that are only required for the one check.
                    return -1
            else:
                # Python sorting is stable, so this will leave everything else in its original order.
                return 0

        progitempool.sort(key=sort_func)
