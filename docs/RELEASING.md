# Publishing a new version

These are the steps to release a new version of RGB HFR Analyzer. N.I.N.A. builds the zip and the manifest for you from the plugin's own `AssemblyInfo.cs`.

1. **Update the version** in `Properties/AssemblyInfo.cs` (`AssemblyVersion` and `AssemblyFileVersion`, always both) and add an entry to `CHANGELOG.md`. Never change the `Guid`.
2. **Build** in Visual Studio with the **Release** configuration (*Build > Rebuild Solution*). The DLL ends up in `bin\Release\net8.0-windows\AntonioAguadoRovira.NINA.Rgbfocus.dll`. Test it in N.I.N.A.
3. **Commit and push** the changes with GitHub Desktop.
4. **Download `tools/CreateManifest.ps1`** from the [plugin manifests repository](https://github.com/isbeorn/nina.plugin.manifests/tree/main/tools) and run it in PowerShell from an empty folder:

   ```powershell
   .\CreateManifest.ps1 -file "C:\path\to\AntonioAguadoRovira.NINA.Rgbfocus.dll" -createArchive -appendVersionToArchive -installerUrl "https://example.com" -beta
   ```

   It creates the `.zip` and a `manifest.json` (with the SHA256 checksum). Leave out `-beta` for a stable release. The `-installerUrl` is a placeholder for now.
5. **Create a release on GitHub** (*Releases > Draft a new release*), tag `v<version>`, and attach **that exact zip**. Do not regenerate it afterwards: a new zip has a different checksum.
6. **Edit `manifest.json`**: replace `Installer.URL` with the real link to the uploaded zip, in the form
   `https://github.com/ToniEjido/RGB-HFR-Analyzer/releases/download/v<version>/<zip file name>`.
7. **Submit it**: fork `isbeorn/nina.plugin.manifests`, add the file as
   `manifests/R/RGB HFR Analyzer/<N.I.N.A. minimum version>/<plugin version>/manifest.json`
   (check the exact folder convention in that repository's README), and open a Pull Request. In the description mention that the plugin was developed with the help of an AI assistant.
