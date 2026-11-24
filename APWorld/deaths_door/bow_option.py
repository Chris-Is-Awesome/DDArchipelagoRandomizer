# Its own module so the logic can use it without importing options.py (which imports the entrances)
from Options import DefaultOnToggle


class StartWithBow(DefaultOnToggle):
    """Start with the Reaper's Bow, like the normal game. Off: the Reaper's Bow is an item in the pool, and you can't
    shoot arrows until you find it. The Arrow Upgrade does nothing without it.
    Without Fire, lighting things needs arrows shot through fires that already burn (Ceramic Manor, the Estate entrance
    and Garden of Joy, the way from the Grove of Spirits door), the Garden of Love pot puzzle and the Frog King need
    the bow, and so do the Offscreen Targeting Tricks."""

    internal_name = "start_with_bow"
    display_name = "Start With Bow"


# Archipelago only unpickles options from an options module (the WebHost sends them pickled), and options.py
# imports this class, so let it be found there
StartWithBow.__module__ = __name__.rsplit(".", 1)[0] + ".options"
