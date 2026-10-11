"""Every shape and colour constant for the BAIT & SWITCH kit. Change values here, re-run build_all.py.

Axes: fish face +X, up is +Z. A fish body is ~0.9 m long (BODY_LEN). Parts attach at sockets on the body.
Each attached part is modelled at the origin "growing" along its local +Z (the socket's outward normal).
"""

# ---------------------------------------------------------------- proportions (fish)
BODY_LEN = 0.9            # nose to tail root, metres
BODY_HEIGHT = 0.70        # chibi: tall, round (big head, short body)
BODY_WIDTH = 0.56
HEAD_BIAS = 0.30          # front half much fatter than the back: reads as a big head
SUBDIV = 2

EEL_LEN_MUL, EEL_HEIGHT_MUL = 1.45, 0.62
PUFFER_SCALE = 1.12
PUFFER_SPIKE_COUNT = 26
PUFFER_SPIKE_LEN = 0.07
FLAT_HEIGHT_MUL, FLAT_WIDTH_MUL = 0.45, 1.25
PLATE_COUNT = 4
PLATE_THICK = 0.018

# eyes
EYE_DIAM = 0.29           # big glossy eyes: ~0.41 of body height
EYE_EMBED = 0.38          # how much of the eyeball sinks into the body
EYE_HEIGHT = 0.06         # (legacy socket_eyes) eye line above body centre
# per-eye sockets: ray directions (forward, side, up) cast at the head; low and toward the front reads as cute
EYE_RAY = (0.62, 0.78, -0.04)
EYE_C_RAY = (0.72, 0.0, 0.62)
PUPIL_FLAT = 0.30         # pupil is a flattened disc pressed onto the eyeball
HIGHLIGHT2_DIAM_MUL = 0.11
EYE_SPREAD = 0.15         # half distance between two eyes
EYE_ASYM = 0.08           # +8% on one eye, -8% on the other (unsettling, deliberately)
PUPIL_DIAM_MUL = 0.66
HIGHLIGHT_DIAM_MUL = 0.26
CYCLOPS_DIAM_MUL = 1.7
SEVEN_EYE_DIAM_MUL = 0.42

# fins / tails
FIN_LEN = 0.20
FIN_WIDTH = 0.12
FIN_THICK = 0.025
BIG_FIN_MUL = 1.6
WING_FIN_LEN_MUL, WING_FIN_WIDTH_MUL = 1.9, 0.8
TAIL_LEN = 0.26
TAIL_SPREAD = 0.22
TAIL_THICK = 0.03

# mouth
MOUTH_WIDTH = 0.12
MOUTH_DEPTH = 0.02
TEETH_COUNT = 9
TOOTH_LEN = 0.035
TOOTH_RADIUS = 0.008
JAW_MUL = 1.8
SUCKER_RADIUS = 0.06

# specials
HAMMER_SPAN = 0.62
HAMMER_THICK = 0.13
TENTACLE_COUNT = 4
TENTACLE_LEN = 0.38
TENTACLE_RADIUS = 0.03
SPINE_COUNT = 7
SPINE_LEN = 0.13
LAMP_STALK = 0.30
LAMP_BULB = 0.07

# ---------------------------------------------------------------- angler gear
COAT_HEIGHT = 0.70
COAT_RADIUS_BOTTOM = 0.46     # three sizes too big: wide bell
COAT_RADIUS_TOP = 0.22
HEM_THICK = 0.04
HOOD_RADIUS = 0.33
HOOD_STRETCH = (1.0, 1.05, 1.12)
HOOD_PEAK_LEN = 0.36          # the cultish point, drooping backwards
HOOD_PEAK_DROOP_DEG = 28
VOID_SCALE = (0.55, 0.86, 0.95)   # the black opening, fraction of the hood radius
HOOD_PUPIL = 0.04
HOOD_PUPIL_SPREAD = 0.085
SLEEVE_LEN = 0.26
SLEEVE_RADIUS = 0.085
BELT_HEIGHT = 0.30            # rope belt above the hem
BELT_THICK = 0.025
ROD_LEN = 1.4
ROD_RADIUS = 0.018
HOOK_SIZE = 0.10
HOOK_WIRE = 0.009
LANTERN_SIZE = 0.09

# ---------------------------------------------------------------- environment / props (camera side is -Y)
KELP_BLADES = 13
KELP_BLADE_LEN = 0.75
KELP_BLADE_WIDTH = 0.24
KELP_STEM = 0.055
CORAL_BRANCHES = 6
CORAL_HEIGHT = 0.95
SHRINE_HEIGHT = 1.1
SIGIL_SIZE = 0.26
CANDLE_RADIUS = 0.065
CANDLE_HEIGHT = 0.24
CRATE_SIZE = (0.8, 0.6, 0.55)
EGG_COUNT = 9
EGG_RADIUS = 0.11

# ---------------------------------------------------------------- scene kit (dinghy, gull)
BOAT_LEN = 1.9
BOAT_WIDTH = 0.9
BOAT_DEPTH = 0.4
BOAT_BOW_PINCH = 0.85
BOAT_STERN_PINCH = 0.25
BOAT_SHEER = 0.55
BOAT_HULL_THICK = 0.035
BOAT_RAIL = 0.03
BOAT_STRAKES = 0
BOAT_PAINT = "#E8E2D2"      # weathered cream (tinted per Angler in Unity)
BOAT_WOOD = "#5A3E2A"
BOAT_TRIM = "#3B2C22"
GULL_HEIGHT = 0.42
GULL_WHITE = "#F2F0EA"
GULL_GREY = "#8E97A3"
GULL_BEAK = "#F2C14E"

# ---------------------------------------------------------------- v2 detail
IRIS_MUL = 0.82           # iris disc diameter vs eyeball
LID_COVER = 0.42          # heavy upper lid on one eye (fraction of the eye it covers)
FIN_SCALLOPS = 5
TAIL_SCALLOPS = 6
SCUTE_COUNT = 6
HULL_STATIONS = 36
HULL_SECTION = 22
HULL_STRAKES = 5
HULL_STEP = 0.012         # clinker step between strakes
HULL_RIBS = 7

# ---------------------------------------------------------------- outline / shading
OUTLINE = 0.012           # inverted-hull thickness, metres (same everywhere)
SMOOTH_ANGLE_DEG = 40

# ---------------------------------------------------------------- budgets (triangles)
BUDGET = {
    "body": 4000, "eyes": 2400, "fins": 1200, "tails": 2000, "mouths": 900, "specials": 2000,
    "angler": 4000, "hooks": 800, "env": 3000, "props": 3000, "scene": 12000,
}

# ---------------------------------------------------------------- palette (linear-ish sRGB hex)
PALETTE = {
    "ink": "#1E1428", "cream": "#F4E9D8", "shallows": "#3FA7A3", "midwater": "#1F5F7A",
    "abyss": "#1B2440", "raincoat": "#F2C14E", "hook_red": "#D64545", "coral": "#FF8C7A",
    "lilac": "#B79CED", "lime": "#B8D86B", "glow_mint": "#9BF6C4", "leviathan": "#C77DFF",
}

# Fill is tinted per fish in Unity; preview uses this grey so shape is judged, not colour.
PREVIEW_FILL = "#B9B3A8"
# Combo/preview tints so contact sheets show how the kit reads in colour (Unity tints per appearance).
PREVIEW_TINTS = ["#FF8C7A", "#B79CED", "#B8D86B", "#3FA7A3", "#F2C14E", "#F4E9D8", "#9BF6C4", "#D9A066"]

# ---------------------------------------------------------------- paths (relative to repo root)
EXPORT_MESH_DIR = "Assets/_Game/Art/Parts"
EXPORT_THUMB_DIR = "Assets/_Game/Art/Thumbs"
OUT_DIR = "tools/blender/out"
