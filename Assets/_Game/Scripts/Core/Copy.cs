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
