# Netcode for Entities

This package allows you to build server authoritative multiplayer games with client prediction using Entities (com.unity.entities).

BUUUUUUUUUUUUUUUUUUUT

After installing it, you'll see errors, and that is okay.

To fix them, follow these steps:

1) Where the stuff is internal, just add
[assembly: InternalsVisibleTo("Zhorman.NestedGhostPrefabs.Runtime.Misc")]
2) Where the stuff is private, you'll unfortunately will have to manually change stuff to public
3) Lastly, you`ll need to change something in the GhostAuthoringComponentBaker:
Somewhere at ~312 you'll need to set the line to be ".WithAll<GhostAuthoringComponentBakingData, LinkedEntityGroup>()"
Somewhere at ~331 you'll need to set the line to be 
"Entities.WithAll<GhostAuthoringComponentBakingData, LinkedEntityGroup>().ForEach((Entity rootEntity, DynamicBuffer<LinkedEntityGroup> linkedEntityGroup, in GhostAuthoringComponentBakingData ghostAuthoringBakingData) =>"
And that should be it.