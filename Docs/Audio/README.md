# Arcade sound review

Run `python Docs/Audio/generate_arcade_audio.py` to reproduce 13 deterministic, original
PCM effects and the cue preview. No third-party music, samples or voices are used.

Listen to [the eleven-second cue preview](arcade_cues_preview.wav). It plays countdown,
GO, gate, lap, final lap, finish, boost, burnout and impact in that order; exact onset times
are in `PREVIEW_TIMELINE.json`. This preview is generated offline and is not a recording
of the Unity mix. Wheel, skid, rocket and garden loops are separate assets under
`Assets/GrannyRacer/Audio/SFX/Arcade` and respond to live race state in the game.

The audit records source peaks and RMS levels. This is a technical check of individual
files, not approval of their musical character, loudness balance or repeated listening.
The effects slider carries across track changes for the current session. Master volume
also controls the original voice; those recordings have not been edited.
