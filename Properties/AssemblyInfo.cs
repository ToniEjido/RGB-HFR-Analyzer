using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin. It must NEVER change.
[assembly: Guid("2a9f66f0-f3c7-485e-8db9-2d1560a17c47")]

// [MANDATORY] The assembly versioning
// Should be incremented for each new release build of a plugin
// 0.9.x.x = public beta (still being tested on real equipment)
[assembly: AssemblyVersion("0.9.0.0")]
[assembly: AssemblyFileVersion("0.9.0.0")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("RGB HFR Analyzer")]
// [MANDATORY] A short description of your plugin (max. 256 characters)
[assembly: AssemblyDescription("See how sharp each colour channel is. HFR per channel (R, G, B, RGB) for OSC cameras, with autofocus on the channel that matters: Ha through a dual-band filter, red on an achromat, or any mix. Works with any telescope.")]

// The following attributes are not required for the plugin per se, but are required by the official manifest meta data

// Your name
[assembly: AssemblyCompany("Antonio Aguado Rovira")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("RGB HFR Analyzer")]
[assembly: AssemblyCopyright("Copyright © 2026 Antonio Aguado Rovira")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.2.0.9001")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
// The repository where your plugin is hosted
[assembly: AssemblyMetadata("Repository", "https://github.com/ToniEjido/RGB-HFR-Analyzer")]

// The following attributes are optional for the official manifest meta data

//[Optional] Your plugin homepage URL - omit if not applicable
[assembly: AssemblyMetadata("Homepage", "https://github.com/ToniEjido/RGB-HFR-Analyzer")]

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "HFR,Focus,Autofocus,Color,OSC,Bayer,Narrowband,Dual band,Ha,Chromatic aberration,Analysis")]

//[Optional] A link that will show a log of all changes in between your plugin's versions
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/ToniEjido/RGB-HFR-Analyzer/blob/main/CHANGELOG.md")]

//[Optional] The url to a featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://raw.githubusercontent.com/ToniEjido/RGB-HFR-Analyzer/main/docs/logo.png")]
//[Optional] A url to an example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "https://raw.githubusercontent.com/ToniEjido/RGB-HFR-Analyzer/main/docs/screenshot.png")]
//[Optional] An additional url to an example example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "https://raw.githubusercontent.com/ToniEjido/RGB-HFR-Analyzer/main/docs/screenshot-autofocus.png")]
//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"BETA. Is your focus really right? RGB HFR Analyzer shows how sharp each colour channel of a one-shot-colour (Bayer) image is, and lets autofocus work on the channel you choose.

N.I.N.A.'s HFR is one number for the whole image. But a colour image is three images, and they are not always equally sharp. This plugin splits every frame into R, G and B, measures each channel with N.I.N.A.'s own debayer and star detector, and shows the HFR per channel next to the global RGB value, so the numbers are directly comparable with the ones you already know.

Who is it for? Anyone with an OSC or DSLR camera, on any telescope:
 - Narrowband and dual/tri-band filters (Ha, Ha+OIII, SII...): the light only reaches some channels. With an Ha filter, focus on red; with Ha+OIII, check red against green+blue. Focusing on the channel that carries your signal, instead of on a global HFR diluted by empty channels, gives better focus where it counts.
 - Refractors, achromatic or ED/APO: see the residual chromatic aberration of your optics, and choose which colour to favour.
 - Reflectors, SCTs and corrected systems: check that colour has not crept in (IR/UV-cut filters, field flatteners, reducers) and use the zone map, tilt and field curvature diagnostics to tune your train.
 - Anyone curious: it is a simple way to learn how your telescope really behaves at each wavelength.

Key features:
 - Analysis panel (Imaging tab): HFR and star count for R, G, B and RGB, updated with every image, saved or not.
 - Viewer of the selected channel with zoom and pan, optional circles on the detected stars, and an optional 3x3 zone map of the HFR.
 - Statistics in a pop-up over the image: mean and median HFR, sigma, error of the mean, tilt and field curvature.
 - Warnings, only when something important is detected (tilt, field curvature, too few stars, high dispersion). Hidden by default.
 - Channel mixes (R+G, G+B, R+B, ...) to look at intermediate focus, or at the two channels of a dual-band filter.
 - Autofocus panel (Tools menu): runs N.I.N.A.'s own autofocus using the HFR of the channel you choose (R, G, B, RGB or a mix of two), with the same curve and graph. A table keeps the best focuser position found for each channel, with a Go button for each one.
 - Optional: use the chosen channel in every N.I.N.A. autofocus, including the sequencer. The HFR N.I.N.A. shows for each image is not affected.
 - Advanced sequencer: a Channel Autofocus instruction and a Channel Change and Refocus trigger that switches channel and refocuses after a number of exposures.
 - File name pattern $$FOCUSCHANNEL$$ with the channel of the last autofocus, to sort your frames by focus channel.
 - English and Spanish interface (follows N.I.N.A.'s language, selectable in Options > Plugins).

It measures; it does not change your images. Nothing is written to FITS headers or files. Requires a colour camera with a Bayer matrix (mono cameras have no channels to separate).

This plugin was developed with the help of an AI assistant (Claude) and tested by its author. Feedback and bug reports are welcome on the GitHub page.")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
