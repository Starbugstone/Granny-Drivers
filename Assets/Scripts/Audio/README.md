# Granny voice reactions

Add `GrannyVoice` to the Granny root GameObject. Unity adds the required
`AudioSource` automatically, and the bundled clips load from `Resources` with no
Inspector setup.

Collision lines fire automatically for impacts at or above the configured speed.
Connect the other events from gameplay code after the action succeeds:

```csharp
grannyVoice.ReactToInput();          // A discrete command, not every held-input frame
grannyVoice.ReactToItemPickup();     // Item entered inventory
grannyVoice.ReactToAttackGiven();    // Granny's hit was confirmed
grannyVoice.ReactToAttackReceived(); // Damage was applied to Granny
```

Each event has five randomized lines and avoids immediately repeating the same
clip. Cooldowns, collision speed, spatial blend, interruption, and replacement
clip banks are configurable in the Inspector.
