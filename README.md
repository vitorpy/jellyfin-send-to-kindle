# Jellyfin Send to Kindle

Send supported books from Jellyfin Web to a Kindle email address. The plugin converts CBR/CBZ comics with KCC, converts PDF/MOBI/AZW/AZW3 books with Calibre, and sends EPUB output through SMTP.

## Requirements

- Jellyfin Server 10.11.x
- [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) for the book-detail action
- `kcc-c2e`, `ebook-convert`, and `7z` available to the Jellyfin service account
- An SMTP sender approved in Amazon's Personal Document Settings

The SMTP password can be stored in plugin configuration or supplied through `JELLYFIN_SEND_TO_KINDLE_SMTP_PASSWORD`. The environment variable takes precedence.

## Installation

1. In Jellyfin, open **Dashboard > Plugins > Repositories**.
2. Add the File Transformation repository:

   ```text
   https://www.iamparadox.dev/jellyfin/plugins/manifest.json
   ```

3. Add the Send to Kindle repository:

   ```text
   https://raw.githubusercontent.com/vitorpy/jellyfin-send-to-kindle/refs/heads/manifest/manifest.json
   ```

4. Open **Catalog** and install **File Transformation** and **Send to Kindle**.
5. Restart Jellyfin.
6. Make `kcc-c2e`, `ebook-convert`, and `7z` available to the Jellyfin service account.
7. Open **Dashboard > Plugins > Send to Kindle**, configure SMTP and conversion settings, then use the diagnostic buttons.
8. Add the configured sender address to the approved senders in Amazon **Manage Your Content and Devices > Preferences > Personal Document Settings**.

The **Send to Kindle** action is shown to administrators on supported Book detail pages. EPUB files are sent directly; CBR/CBZ files use KCC; PDF/MOBI/AZW/AZW3 files use Calibre.

## Plugin Repository

Tagged releases publish the versioned ZIP to GitHub Releases and update the Jellyfin repository manifest on the `manifest` branch.

## Development

```bash
dotnet restore Jellyfin.Plugin.SendToKindle.slnx
dotnet build Jellyfin.Plugin.SendToKindle.slnx --configuration Release --no-restore
dotnet test Jellyfin.Plugin.SendToKindle.slnx --configuration Release --no-build
```

Create releases with four-part version tags such as `v1.0.0.0`. The tag must match `Directory.Build.props` and `build.yaml`.

The CI workflow builds, tests, and uploads an installable ZIP for every main-branch commit and pull request. The Release workflow attaches the versioned ZIP to a GitHub Release and prepends it to the Jellyfin manifest.

## License

GPL-3.0-or-later
