---
description: Zip the Windows build and push it to itch with butler
---
1. Ask me to make the Windows build into `builds/win/` (File > Build Profiles) and wait for "done".
2. Remove any `*_BurstDebugInformation_DoNotShip` folder from `builds/win/`.
3. Ask before running: `butler push builds/win <itch-user>/bait-and-switch:windows --userversion <UTC timestamp>`
   (I'll fill in the itch user the first time; remember it for this session).
4. Reply in at most 3 lines. Do not run git.
