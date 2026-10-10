"""Every shape and colour constant for the BAIT & SWITCH kit. Change values here, re-run build_all.py.

Axes: fish face +X, up is +Z. A fish body is ~0.9 m long (BODY_LEN). Parts attach at sockets on the body.
Each attached part is modelled at the origin "growing" along its local +Z (the socket's outward normal).
"""

# ---------------------------------------------------------------- proportions (fish)
BODY_LEN = 0.9            # nose to tail root, metres
BODY_HEIGHT = 0.62        # chibi: tall, round
BODY_WIDTH = 0.52
HEAD_BIAS = 0.18          # front half fatter than the back (0 = symmetric egg)
SUBDIV = 2

EEL_LEN_MUL, EEL_HEIGHT_MUL = 1.45, 0.62
PUFFER_SCALE = 1.12
PUFFER_SPIKE_COUNT = 26
PUFFER_SPIKE_LEN = 0.07
FLAT_HEIGHT_MUL, FLAT_WIDTH_MUL = 0.45, 1.25
PLATE_COUNT = 4
PLATE_THICK = 0.018

# eyes
EYE_DIAM = 0.20           # big glossy eyes: fraction of body height ~ 0.32
EYE_EMBED = 0.30          # 30% of the eyeball sinks into the body
EYE_HEIGHT = 0.06         # eye line above body centre (low on the face reads as cute)
EYE_SPREAD = 0.15         # half distance between two eyes
EYE_ASYM = 0.08           # +8% on one eye, -8% on the other (unsettling, deliberately)
PUPIL_DIAM_MUL = 0.55
HIGHLIGHT_DIAM_MUL = 0.22
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
COAT_RADIUS_BOTTOM = 0.42
COAT_RADIUS_TOP = 0.20
HOOD_RADIUS = 0.30
ROD_LEN = 1.4
ROD_RADIUS = 0.018
HOOK_SIZE = 0.10
HOOK_WIRE = 0.009
LANTERN_SIZE = 0.09

# ---------------------------------------------------------------- outline / shading
OUTLINE = 0.012           # inverted-hull thickness, metres (same everywhere)
SMOOTH_ANGLE_DEG = 40

# ---------------------------------------------------------------- budgets (triangles)
BUDGET = {
    "body": 1800, "eyes": 1000, "fins": 250, "tails": 500, "mouths": 450, "specials": 900,
    "angler": 1600, "hooks": 400, "env": 900, "props": 900,
}

# ---------------------------------------------------------------- palette (linear-ish sRGB hex)
PALETTE = {
    "ink": "#1E1428", "cream": "#F4E9D8", "shallows": "#3FA7A3", "midwater": "#1F5F7A",
    "abyss": "#1B2440", "raincoat": "#F2C14E", "hook_red": "#D64545", "coral": "#FF8C7A",
    "lilac": "#B79CED", "lime": "#B8D86B", "glow_mint": "#9BF6C4", "leviathan": "#C77DFF",
}

# Fill is tinted per fish in Unity; preview uses this grey so shape is judged, not colour.
PREVIEW_FILL = "#B9B3A8"

# ---------------------------------------------------------------- paths (relative to repo root)
EXPORT_MESH_DIR = "Assets/_Game/Art/Parts"
EXPORT_THUMB_DIR = "Assets/_Game/Art/Thumbs"
OUT_DIR = "tools/blender/out"
