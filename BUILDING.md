# Building

The project imports shared MSBuild props from the [ValheimModBuild](https://github.com/dsoltyka/ValheimModBuild) repo, which must be cloned next to this one:

```
<parent>/
  ValheimModBuild/
  ValheimLiveExperienceTracker/
```

Then:

```
dotnet build                                          # build + copy DLL into your Thunderstore profile
dotnet build -c Release -p:ThunderstorePack=true      # also write thunderstore/manifest.json and the upload zip
```

Paths to the game and the BepInEx profile are configured once in `ValheimModBuild/Valheim.Local.props` (see that repo's README).

## Releasing

1. Bump `<Version>` in `LiveExperienceTracker.csproj` and add a `CHANGELOG.md` entry.
2. Run the pack command above. It regenerates `thunderstore/manifest.json` from the csproj and writes `thunderstore/LiveExperienceTracker-<version>.zip`.
3. Commit, tag `v<version>`, push, and upload the zip through the normal Thunderstore upload form under the `dsoltyka` team.

Thunderstore renders `README.md` from inside the zip as the mod page, so keep that file user-facing. Images in it must use absolute URLs (the screenshot points at the raw GitHub file).

