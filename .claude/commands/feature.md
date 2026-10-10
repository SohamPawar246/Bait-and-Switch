---
description: Build one feature from the docs with minimal token use
argument-hint: <feature> <docs/file.md ...>
---
Feature: $ARGUMENTS

1. Read CLAUDE.md rules (already loaded) and ONLY the docs files named above, plus the existing scripts
   you must change. Don't explore further.
2. Write a plan of at most 8 lines: files to create/change and what each does. Then implement it fully
   without stopping, in complete files.
3. Check the Unity console for compile errors ONCE. Fix and check once more if needed, then stop.
4. Reply with only: files changed (one line each), "Test:" (max 5 bullets for me), and
   "Commit: <type>: <summary>". Do not run git.
