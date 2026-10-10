---
description: Build the Linux server, push to Edgegap, point a version at it
argument-hint: <dev|prod>
---
Ship the server to Edgegap version: $ARGUMENTS (default dev).

1. Ask me to run "Build server" + "Containerize" + "Upload" from the Edgegap Unity plugin window
   (it owns the build pipeline) with image tag = current UTC timestamp, and wait for my "done".
2. Using the Edgegap MCP: update app `bait-and-switch` version `$ARGUMENTS` to the new tag
   (UDP port 7777 named gameport, restart policy Never). Don't create new apps or versions.
3. Do NOT start a deployment unless I say so. If I do, wait for it with the MCP's wait tool,
   report host:port and location, and stop it when I say testing is done.
4. Reply in at most 5 lines. Do not run git.
