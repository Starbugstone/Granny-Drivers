# Granny racer profiles

The race uses one `ArcadeWalkerController` as the reusable physics base. A selected
`GrannyRacerProfile` supplies three multipliers over its `WalkerHandlingSettings`:

- **Acceleration** affects normal, reverse, and boosted acceleration.
- **Adherence** affects lateral grip in normal driving, boost, skid, and drift.
- **Maximum speed** affects normal, reverse, boosted, and drift-boost speed limits.

Keeping the base tuning and character differences separate means the common walker can be
retuned without editing every granny. A missing profile is deliberately the balanced 1.0x
fallback, so the current POC remains compatible.

## Unity setup

1. In the Project window, choose **Create > Granny Racer > Granny Racer Profile**.
2. Name the asset for the granny and tune the three multipliers. Start with small differences
   (roughly 0.85–1.15) so one stat does not dominate the whole race.
3. For a fixed/default granny, drag the profile onto the **Profile** field of the base racer’s
   `ArcadeWalkerController`.
4. When the character-selection screen is implemented, keep its chosen profile reference and
   call `ArcadeWalkerController.ApplyProfile(chosenProfile)` before the countdown/reset.

The roster UI, persistence, portraits, and additional finished granny models are deferred.
They should consume these profile assets rather than introduce granny-specific controllers.

Human playtesting is still required to decide whether each profile feels distinct and fair.
