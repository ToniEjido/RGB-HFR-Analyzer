# Publishing a new version

These are the steps to release a new version of RGB HFR Analyzer. N.I.N.A. builds the zip and the manifest for you from the plugin's own `AssemblyInfo.cs`.

The rule that matters most: **the release tag must point to the exact commit the DLL was built from.** Never build, then keep committing, then tag.

1. **Update the version** in `Properties/AssemblyInfo.cs` (`AssemblyVersion` and `AssemblyFileVersion`, always both) and add an entry to `CHANGELOG.md`. Never change the `Guid`.
2. **Run the tests**: `dotnet test Tests` (or *Test > Run All Tests* in Visual Studio). They do not need N.I.N.A.
3. **Commit and push everything** with GitHub Desktop (*Commit to main*, then *Push origin*). From here on, change nothing: if you must change something, go back to step 1.
4. **Build** in Visual Studio with the **Release** configuration (*Build > Rebuild Solution*). The DLL ends up in `bin\Release\net8.0-windows\RGBHFRAnalyzer.dll`. Test it in N.I.N.A.
5. **Create the zip and the manifest.** Download `tools/CreateManifest.ps1` from the [plugin manifests repository](https://github.com/isbeorn/nina.plugin.manifests/tree/main/tools) and run it in PowerShell from an empty folder:

   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   .\CreateManifest.ps1 -file "C:\path\to\RGBHFRAnalyzer.dll" -createArchive -appendVersionToArchive -installerUrl "https://example.com" -beta
   ```

   It creates the `.zip` and a `manifest.json` (with the SHA256 checksum). Leave out `-beta` for a stable release. The `-installerUrl` is a placeholder for now.
6. **Create the release on GitHub** (*Releases > Draft a new release*):
   - Tag: `v<version>` with **Create new tag on publish**, target **main**.
   - Attach **that exact zip**. Do not regenerate it afterwards: a new zip has a different checksum.
   - After publishing, check that the release page shows the same short commit id as the latest commit in GitHub Desktop (*History*). If it does not, delete the release and the tag and repeat.
7. **Edit `manifest.json`**: replace `Installer.URL` with the real link to the uploaded zip, in the form
   `https://github.com/ToniEjido/RGB-HFR-Analyzer/releases/download/v<version>/<zip file name>`,
   and check that `Installer.Checksum` equals `Get-FileHash <zip> -Algorithm SHA256`.
8. **Submit it**: fork `isbeorn/nina.plugin.manifests`, add the file as
   `manifests/r/RGB HFR Analyzer/<N.I.N.A. minimum version>/<plugin version>/manifest.json`
   (for example `manifests/r/RGB HFR Analyzer/3.2.0/0.9.1.0/manifest.json`) and open a Pull Request. In the description mention that the plugin was developed with the help of an AI assistant.
   If an earlier version of this plugin is still waiting in an open Pull Request, replace its file instead of adding a second one.
