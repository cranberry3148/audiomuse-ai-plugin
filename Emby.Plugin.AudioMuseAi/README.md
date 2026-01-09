# AudioMuse AI - Emby Plugin

This is the **Emby** version of the AudioMuse AI plugin. It integrates core AudioMuse-AI features into Emby, providing advanced music analysis and "Instant Mix" capabilities.

## Features

*   **Instant Mix Implementation**: Provides a smart Instant Mix endpoint at `/AudioMuseAI/InstantMix/{Id}`.
*   **Scheduled Tasks**: Includes tasks for Audio Analysis and Clustering, synchronized with your AudioMuse backend.
*   **Shared Backend**: Connects to the same AudioMuse AI container as the Jellyfin plugin.

## Instant Mix Override (Important)

Unlike the Jellyfin version, the Emby plugin cannot automatically override the native `/Items/{Id}/InstantMix` endpoint. To use AudioMuse AI for Instant Mixes in Emby, you must configure a **reverse proxy rule** to redirect requests.

### Nginx Example

Add the following to your Nginx configuration:

```nginx
location ~ ^/emby/Items/([^/]+)/InstantMix$ {
    set $itemid $1;

    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    # In case the upstream uses a self-signed/invalid cert
    proxy_ssl_verify off;

    # Redirect to the AudioMuse AI custom endpoint
    proxy_pass https://127.0.0.1:8096/emby/AudioMuseAI/InstantMix/$itemid$is_args$args;
}
```
*(Replace `127.0.0.1:8096` with your actual Emby server address and port)*

## Build Instructions

This project uses the official Emby NuGet packages, so it can be built like any standard .NET project.

### 1. Build with Dotnet
Run the standard build command:

```bash
dotnet build -c Release Emby.Plugin.AudioMuseAi/Emby.Plugin.AudioMuseAi.csproj
```

### 2. Install
Copy the output DLLs to your Emby Server's `plugins` folder.
*   `Emby.Plugin.AudioMuseAi.dll`
*   `AudioMuseAi.Common.dll`

Restart Emby Server to load the plugin.

## Configuration

After installation:
1.  Go to **Emby Dashboard** > **Plugins**.
2.  Click on **AudioMuse AI**.
3.  Enter the URL of your **AudioMuse AI** backend (e.g., `http://192.168.1.10:8000`).
4.  Save and run the **Analysis** scheduled task.

## Troubleshooting

*   **Instant Mix not working?** Check the Emby logs for `AudioMuseAI`. The plugin logs details about connection to the backend and item lookups.
*   **Item Mismatch?** If the backend returns IDs that Emby doesn't recognize (e.g., from a different library scan), the plugin automatically attempts to fall back to searching by Title and Artist to remap the tracks.
