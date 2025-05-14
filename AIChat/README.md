# DNN PersonaBar AI Chat Extension (Vue.js + Anthropic)

This extension adds an AI-powered chat to the DNN PersonaBar, using a Vue.js frontend and the Anthropic SDK for C#.

## Structure

```
/AIChat/                  # C# backend (class library)
/DesktopModules/Admin/AIChat/  # Vue.js frontend build output
AIChat.dnn                # DNN manifest
```

## Backend (C#)
- Implements a PersonaBar menu and a Web API controller that proxies chat requests to Anthropic.
- Uses [Anthropic.SDK](https://github.com/tghamm/Anthropic.SDK) (install via NuGet).

## Frontend (Vue.js)
- Simple chat UI that calls the backend API.
- Built with Vite, output goes to `/DesktopModules/Admin/AIChat/`.

## Setup Steps

1. **Build the C# project** in `/AIChat/` and copy the DLL to your DNN site's `/bin/`.
2. **Build the Vue app** and copy the output to `/DesktopModules/Admin/AIChat/` in your DNN site.
3. **Copy `AIChat.dnn`** to your DNN site's `/Install/Module/` or root.
4. **Install the extension** via DNN's Extensions page.
5. **Set your Anthropic API key** as an environment variable (`ANTHROPIC_API_KEY`) or in your config.

## Usage
- Log in as Host/SuperUser.
- Open the PersonaBar and select "AI Chat".
- Start chatting with Claude via Anthropic!

---

For more details, see the code comments and manifest. 