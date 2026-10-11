namespace BaitSwitch
{
    /// <summary>
    /// Every player-facing line, in one voice: deadpan, earnest, quietly sinister (docs/01-art-direction.md).
    /// Add new text here, never inline in UI code.
    /// </summary>
    public static class Copy
    {
        // ---- Main menu
        public const string Title = "BAIT & SWITCH";
        public const string Tagline = "Hide in the school. Steal the bait. Don't get switched.";
        public const string MenuDive = "Dive in";
        public const string MenuWorkbench = "Workbench";
        public const string MenuSettings = "Settings";
        public const string MenuQuit = "Leave the shore";
        public const string JamCredit = "Overpowered Jam #1 · hosted on Edgegap";

        // ---- Boot loader (descending line)
        public const string BootCalling = "Calling the Deep…";
        public const string BootStirs = "A vessel stirs near {0}…";
        public const string BootSomewhere = "somewhere cold";
        public const string BootRotating = "The old vessel sinks. Another rises…";
        public const string BootLanterns = "Lighting the lanterns…";
        public const string BootOpen = "The sea is open.";
        public const string BootSlow = "The Deep is slow tonight…";
        public const string BootBusy = "The Deep is busy. Try again.";
        public const string BootBrokerDown = "The shore is unreachable. Check your connection.";
        public const string BootTimeout = "The Deep did not answer.";
        public const string Retry = "Try again";
        public const string ClickToBegin = "Click to begin";
        public const string RosterTitle = "In the sea · {0} of {1} players";
        public const string RoleFish = "Fish";
        public const string RoleAngler = "Angler";
        public const string RoleRobing = "Being robed";
        public const string RosterYou = "  (you)";
        public const string RosterBot = "  · bot";
        public const string NibblePrompt = "Hold LMB to nibble";
        public const string BootDeepSilent = "The Deep does not answer tonight.";
        public const string PlayOffline = "Fish alone with the bots";
        public const string Reconnecting = "Reconnecting…";
        public const string Rotation = "Moving to a fresh server…";
        public const string ServerPanel = "Edgegap · {0} · {1} ms";

        // ---- Workbench
        public const string Hatchery = "Hatchery";
        public const string Vestry = "Vestry";
        public const string SpecimenCard = "Specimen No. {0}";
        public const string VowsCard = "Vows of {0}";
        public const string Randomize = "Let the Deep decide";
        public const string Joined = "{0} has entered the water.";
        public static readonly string[] FishStats = { "Speed", "Stealth", "Stamina", "Bite" };
        public static readonly string[] AnglerStats = { "Reach", "Grip", "Line strength", "Lure" };
        public static readonly string[] NameSuggestions =
        {
            "Doomed Gerald", "Kevin (Brine)", "Brother Unworthy", "Small Regret", "Sister Soggy",
            "Not Bait", "Mostly Gills", "Deacon Damp", "The Last Sardine", "Wet Steve",
            "Barely Herring", "Pastor Plankton", "Fish No. 9", "Lent Fillet", "Sincere Carp",
        };

        // ---- Round
        public const string IntroFish = "You are a fish. Do not be chosen.";
        public const string IntroAngler = "You are an Angler. Bring them into the light.";
        public const string IntroFishHint = "Swim like the school. Steal bait. Survive.";
        public const string IntroAnglerHint = "Find the fish that moves wrong. Strike when the line twitches.";
        public const string Appetite = "Tonight the Deep craves: {0}";
        public const string FishLeft = "Fish remaining: {0}";
        public const string Dusk = "The light thins.";
        public const string Night = "The light leaves. Something else arrives.";
        public const string Saved = "YOU HAVE BEEN SAVED.";
        public const string SnapAngler = "The line has failed you.";
        public const string SnapFish = "Not today.";
        public const string LeviathanRises = "IT HUNGERS.";
        public const string BoatTaken = "The Deep has accepted your offering.";
        public const string SomethingNew = "Something new swims among you.";
        public const string FishWin = "The sea keeps its secrets.";
        public const string AnglersWin = "All are saved. Praise the Deep.";

        // ---- HUD (greybox until the real UI)
        public const string HudWaiting = "The sea is empty. The Deep waits for you.";
        public const string HudRound = "Round {0} of {1}";
        public const string HudClock = "{0}:{1:00}";
        public const string HudDay = "Day";
        public const string HudDusk = "Dusk";
        public const string HudNight = "Night";
        public const string HudYouFish = "You are a fish.";
        public const string HudYouAngler = "You are an Angler.";
        public const string HudConverting = "The robing is in progress.";
        public const string HudSpectating = "You watch from the shore.";
        public const string HudTension = "Tension";
        public const string HudStamina = "Stamina";
        public const string HudExhausted = "Exhausted";
        public const string HudPoints = "+{0}";
        // what you are for, spelled out (intro card, hold H, the goal line, tug hints)
        public static readonly string[] GoalFish =
        {
            "Hide. Swim slowly with the school: dashing and straight lines give you away.",
            "Nibble bait (hold LMB beside a hook) for points, then slip away before the strike.",
            "Hooked? Swim AWAY from the boat to pull, rest when tired, dash to snap the line.",
            "Survive until dawn. Caught fish put on the hood and fish for the cult.",
        };
        public static readonly string[] GoalAngler =
        {
            "Most fish are the school. A few are players, and they swim a little wrong.",
            "Lower your hook with the mouse. When the line twitches, LMB to strike.",
            "In the tug, hold LMB to reel while the fish rests. Ease off when the line turns red.",
            "Every fish you land joins you. Save them all before dawn.",
        };
        public const string GoalLeviathan = "You have grown into a Leviathan. Swim under a boat and hold LMB to drag it down.";
        public const string GoalShortFish = "Blend in · nibble bait · survive until dawn";
        public const string GoalShortAngler = "Spot the fish that swim wrong · hook them · reel them in";
        public const string GoalShortLeviathan = "Swim under a boat · hold LMB · drag it down";
        public const string GoalShortConverting = "You were caught. Next round you fish for the cult.";
        public const string HoldForRules = "Hold H for the rules";
        public const string RoundBegins = "The round begins in {0}";
        public const string TugTitle = "THE TUG";
        public const string TugHintFish = "Swim away from the boat to pull · rest to recover · Space to snap the line";
        public const string TugHintAngler = "Hold LMB to reel · ease off when the line goes red";
        public const string LedgerYou = "you";
        public const string FeedReeled = "{0} was reeled in by {1}. The hood fits.";
        public const string FeedReeledAlone = "{0} was reeled in. The hood fits.";
        public const string HudBlendGood = "The school accepts you.";
        public const string HudBlendBad = "You are being noticed.";
        public const string HudControlsFish = "WASD swim · hold LMB nibble · Space dash · E ink · hold Tab to find yourself";
        public const string HudControlsAngler = "A/D row · mouse hook · LMB strike/reel · RMB rebait · Q sonar · MMB mark";
        public const string HudControlsLeviathan = "Swim under a boat · hold LMB to bite";
        public const string Tangled = "Your line is tangled. Patience is a virtue.";
        public const string TornLoose = "It tore loose. The Deep shrugs.";
        public const string Escaped = "You slipped the hook.";
        public const string BaitTaken = "The bait is gone.";
        public const string SonarPing = "A ping rings out across the water.";
        public const string LedgerColumns = "Name · Points · Catches · Escapes";
        public const string LedgerWinner = "The Deep favours {0}.";

        // ---- Screens
        public const string SkipHint = "Click to skip";
        public const string SettingsTitle = "Settings";
        public const string Master = "Master volume";
        public const string MusicVol = "Music";
        public const string EffectsVol = "Effects";
        public const string SwimMouse = "Swim toward the mouse";
        public const string FullscreenOpt = "Fullscreen";
        public const string SystemCursorOpt = "Plain cursor (no hook)";
        public const string Back = "Back";
        public const string PauseTitle = "The tide holds its breath.";
        public const string PauseHint = "The sea does not pause. Be quick.";
        public const string Resume = "Return to the water";
        public const string LeaveMatch = "Leave the shore";
        public const string Cancel = "Reel in";
        public const string HostLocal = "Host a local sea";
        public const string JoinLocal = "Join a local sea";
        public const string StatusLocal = "Local waters · no broker configured";
        public const string StatusBroker = "Edgegap · the Deep answers on demand";
        public const string Disconnected = "The line went slack.";
        public const string WorkbenchHint = "Click a card to wear it, or drag it onto a glowing mark. Drag a fitted part off the creature to remove it.";
        public const string WorkbenchClickHint = "Click to wear  ·  drag onto a glowing mark  ·  drag off to remove";
        public const string WorkbenchEyesHint = "Eyes snap to the face. Drag one off to pluck it.";
        public const string NeedAnEye = "The Deep insists on at least one eye.";
        public const string DiveIn = "Dive in";
        public const string NameLabel = "Your name in the Book";
        public const string TabBody = "Body";
        public const string TabEyes = "Eyes";
        public const string TabFins = "Fins";
        public const string TabTail = "Tail";
        public const string TabMouth = "Mouth";
        public const string TabSpecial = "Oddity";
        public const string TabPaint = "Paint";
        public const string TabHat = "Hat";
        public const string TabRod = "Rod";
        public const string TabHook = "Hook";
        public const string TabBait = "Bait";
        public const string TabRelic = "Relic";
        public const string Snapped = "It fits. Uncomfortably.";
        public const string Removed = "Returned to the Deep.";
        public const string Eye = "Eye";
        public const string EyeCluster = "Cluster of eyes";

        // ---- Book of Rites (how to play)
        public const string MenuHowTo = "How to play";
        public const string RitesHeader = "The Book of Rites";
        public const string RitesKeys = "Hands of the faithful";
        public const string NextPage = "Turn the page";
        public const string RiteGoalTitle = "The Deep Is Hungry";
        public const string RiteGoal = "Six souls enter the water. One is an <b>Angler</b>, a hooded fisher of the Deep. The rest are <b>fish</b>, hiding among 150 identical, innocent fish.\n\nEvery fish the Angler lands is <b>saved</b>: it pulls on a raincoat and joins the hunt. By the end, the sea is mostly raincoats.\n\nThree rounds of two minutes. The most points after three rounds wins. Roles change every round.";
        public const string RiteGoalKeys = "Hide. Hunt. Be saved. Repeat.";
        public const string RiteFishTitle = "If You Are a Fish";
        public const string RiteFish = "<b>Act natural.</b> The school drifts at an easy pace. Swim with it and you are invisible.\n\nHold the mouse beside a bait to <b>nibble</b> (+2, and you grow). Every nibble twitches the Angler's line, but school fish nibble too, so they can never be sure.\n\nSurvive the round: <b>+5</b>. Be the last fish swimming: <b>+5 more</b>.\n\nThe school never dashes. Neither should you, unless you must.";
        public const string RiteAnglerTitle = "If You Are an Angler";
        public const string RiteAngler = "Row along the surface and steer the hook with the mouse. It swings on a weighted line, so be gentle.\n\nPark your bait in the school and watch the line. When it <b>twitches</b>, something is nibbling. <b>Click to strike.</b>\n\nA school fish is worth <b>+1</b>. A real one starts a <b>tug-of-war</b> worth <b>+10</b>.\n\nHunt the fish that moves wrong: the one that dashes, lingers, inks, or has grown too big.";
        public const string RiteTugTitle = "The Tug-of-War";
        public const string RiteTug = "A hooked player fights back, and both of you see the tension.\n\n<b>Angler:</b> hold click to reel. Reel while the fish rests, ease off while it pulls. If tension hits the top, the line snaps.\n\n<b>Fish:</b> swim away from the boat to pull, rest to recover stamina, and <b>dash</b> when tension is high to snap the line (+3).\n\nAfter 12 seconds the hook tears loose on its own. The Deep is patient, but not that patient.";
        public const string RiteTugKeys = "Angler: hold LMB to reel\nFish: swim away · rest · Space to dash";
        public const string RiteLeviathanTitle = "No Limits";
        public const string RiteLeviathan = "There is no limit to how big a fish can grow. Eat six baits and you become a <b>Leviathan</b>: huge, slow, impossible to hide.\n\nOne hook cannot hold a Leviathan. It takes <b>two Anglers at once</b>.\n\nSwim under a boat and hold the mouse to <b>bite</b>: the boat goes under and its Angler becomes a fish again. <b>+10</b>. The Deep has accepted your offering.";
        public const string RiteWinTitle = "How It Ends";
        public const string RiteWin = "The <b>Anglers win the round</b> if every fish is saved before the clock runs out. The <b>fish win</b> if anyone is still swimming at 2:00.\n\nPoints carry across all three rounds, whichever side you end up on. Being saved early is a setback, never the end.\n\nAfter the third round the <b>Book of Tithes</b> names the winner. Praise the Deep.";
        public const string RiteWinKeys = "Esc pauses. The sea does not.";
        public static readonly string[] TipsFish =
        {
            "Swim with the school. Fast fish get noticed.",
            "Hold LMB beside a bait to nibble. Let go if the line tenses.",
            "Green text bottom-left: you blend in. Red: you are being noticed.",
        };
        public static readonly string[] TipsAngler =
        {
            "Park your hook inside the school and keep it still.",
            "When the line twitches, click to strike. Hold RMB to rebait.",
            "Q sends a sonar ping. Middle-click marks a suspect for other Anglers.",
        };

        // ---- Ledger
        public const string LedgerHeader = "Book of Tithes, Round {0}";
        public const string LedgerFinal = "Book of Tithes, Final Reckoning";
        public const string TitleBiggest = "Biggest Fish";
        public const string TitleHook = "Hook of Destiny";
        public const string TitleSlippery = "Slipperiest";
        public const string TitleLeviathan = "Leviathan";
        public const string PlayAgain = "Return to the water";
        public static readonly string[] LedgerFooters =
        {
            "Rule 7: the Deep is never full.",
            "Tithes are final. Refunds are not a concept.",
            "Unlanded fish will be remembered. Briefly.",
        };

        // ---- Bots
        public static readonly string[] BotAnglers =
        {
            "Brother Haddock", "Sister Brine", "Deacon Krill", "Mother Marrow", "Father Fathom", "Elder Gill",
        };
        public const string BotFish = "Fish No. {0}";

        // ---- Loading tips
        public static readonly string[] Tips =
        {
            "Bait is a test of faith. Most fail.",
            "The school does not dash. Neither should you.",
            "Every Angler was a fish once. Every single one.",
            "Do not look the Leviathan in any of its eyes.",
            "A still hook is a patient hook.",
            "The ink hides you. It also tells them where you hid.",
            "Pull when they reel. Rest when they wait. Then dash.",
            "Rule 3: what is landed stays landed.",
            "The Deep notices what you wear. So do they.",
        };
    }
}
