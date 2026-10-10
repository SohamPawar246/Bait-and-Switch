---
description: Fix one bug with a single focused investigation
argument-hint: <what happens vs what should happen, plus any error text>
---
Bug: $ARGUMENTS

1. Form at most 3 hypotheses from the description and the error text. Read only the files they point to.
2. Pick the most likely cause, state it in one line, and fix it. Don't refactor unrelated code.
3. Check the Unity console once. If still broken after 2 attempts, stop and tell me what you ruled out
   and what log line or test would settle it. Do not loop.
4. Reply: cause (1 line), files changed, "Test:" (max 3 bullets), "Commit: fix: <summary>". Do not run git.
