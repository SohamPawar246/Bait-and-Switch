---
description: Rebuild one Blender part family, validate, show one contact sheet
argument-hint: <family: bodies|eyes|fins|tails|mouths|specials|angler|env|props|all>
---
Family: $ARGUMENTS

Follow tools/blender/CLAUDE.md. Edit only the script for this family and style.py if needed.
Then via the Blender MCP run, in one execution:
`exec(open(r"<repo>/tools/blender/build_all.py").read())` with FAMILY="$ARGUMENTS",
which builds, validates and renders ONE contact sheet. Read report.json; fix validation failures
(max 2 rounds), then show me the sheet and name its 3 most visible flaws. Do not export until I approve.
On "approved": run export for this family only. Reply briefly. Do not run git.
