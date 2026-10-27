# Changelog

## 0.9.1.0 (beta)
Changes requested in the review of the manifest pull request:
- The channel is fixed during an autofocus run. A run now locks its channel and the plugin's star detector until it ends: the channel selector is disabled and the displayed selection follows the channel being measured, so changes from the panel or from another sequencer item can no longer mix channels inside one focus curve. Only one channel autofocus can run at a time.
- Per-channel autofocus no longer falls back silently to ordinary detection. If the requested channel cannot be measured (mono camera, or a colour camera with binning, which includes OSC cameras using monochrome binning) it checks before starting and fails with a clear message. The channel is only recorded in `$$FOCUSCHANNEL$$` after an autofocus that actually measured it.
- The Channel Change and Refocus trigger checks `ItemUtility.IsTooCloseToMeridianFlip`, like N.I.N.A.'s autofocus triggers, and postpones the autofocus when it could not finish before a scheduled meridian flip.
- Added unit tests (`Tests` folder, NUnit) for these cases.

Also:
- Selecting an image from N.I.N.A.'s image history shows the result calculated when it was captured, as N.I.N.A. does with its own HFR, instead of recalculating it from the reloaded file (a reloaded RAW is not identical to the captured frame, so the values differed slightly).
- Star symbol next to the star counts, and clearer icons.

## 0.9.0.0 (beta)
- First public beta.
- Per-channel HFR analysis (R, G, B, RGB) using N.I.N.A.'s own debayer and star detector.
- Channel viewer with zoom and pan, star markers, 3x3 zone map, statistics pop-up and optional warnings.
- Channel mixes (R+G, G+B, R+B, ...) for intermediate focus.
- Per-channel autofocus panel (R, G, B, RGB or a mix of two channels) using N.I.N.A.'s autofocus engine, with a table of the best position per channel and "Go" buttons.
- Option to use the chosen channel in every N.I.N.A. autofocus, including the sequencer. The HFR shown for each image is not affected.
- Advanced sequencer: Channel Autofocus instruction and Channel Change and Refocus trigger.
- File name pattern `$$FOCUSCHANNEL$$`.
- English and Spanish interface, selectable in Options > Plugins.
