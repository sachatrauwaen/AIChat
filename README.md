# DNN AI Assistant 

This extension adds an AI-powered chat to the DNN PersonaBar and exposes DNN as an **MCP (Model Context Protocol) server** so external AI clients can use your portal's tools.

**In-portal chat** uses multiple LLM providers (Anthropic Claude, Google Gemini, OpenAI, and more) via LlmTornado. You need an API key from your chosen provider (e.g. [Anthropic](https://console.anthropic.com)) to use the built-in chat.

**MCP server** allows clients such as [Goose](https://block.github.io/goose/), Cursor, GitHub Copilot, and Claude Desktop to connect to your DNN portal and use the same tools (pages, modules, files, Open Content, etc.) with Bearer token authentication.

**Beta version. Make a backup before use.**

---

<img width="467" height="230" alt="image" src="https://github.com/user-attachments/assets/be0346a9-12fc-47f0-b318-4aa356979bf5" />



## Features

### In-portal AI Chat
- Chat with your DNN portal from the PersonaBar.
- **Multiple LLM providers**: choose from Anthropic (Claude), Google (Gemini), OpenAI (GPT), Zai (GLM), and other models supported by LlmTornado.
- Configurable model, max tokens, and conversation history limits.

### MCP (Model Context Protocol) server
- Expose your DNN portal as an MCP server so external AI clients can connect and use DNN tools.
- **Bearer API key** authentication: generate and manage keys with optional validity (e.g. 7 days, 90 days, never expires).
- **Client configuration** guides for Goose, Cursor, GitHub Copilot, and Claude Desktop.
- **Tool selection**: enable or disable which tools are available per portal.
- **Custom rules/prompts**: define prompts in `mcp/prompts` and manage them from MCP Settings.

### Tools
- **Portal management**: pages (add, get, update, delete), modules (add, get, update), HTML module content, folders and files, system files, send email, get URL HTML, get URL SEO.
- **Open Content** (when [OpenContentMcp](https://github.com/sachatrauwaen/OpenContentAICHat) is installed): get, add, update Open Content items; manage templates.
- **SEO**: URL SEO tool (when SeoTools is installed).
- Tools are shared between the in-portal chat and MCP clients; add your own by implementing `IMcpProvider` and registering tools with `IMcpRegistry`. Example: [Open Content AIChat](https://github.com/sachatrauwaen/OpenContentAICHat).

## Settings

**AI Settings** (PersonaBar → AI Assistant): model, API key for the chosen provider, max tokens, history limits, and which tools are active for the in-portal chat.

**MCP Settings** (PersonaBar → MCP): API key (generate/copy), validity delay, active tools for MCP, custom rules/prompts, and client-specific configuration (Goose, Cursor, Copilot, Claude Desktop).

<img width="372" height="460" alt="image" src="https://github.com/user-attachments/assets/95c2b4ad-ea0a-486b-a991-e5e8588bee36" />

---
# Documentation

[https://github.com/sachatrauwaen/AIChat/wiki](https://github.com/sachatrauwaen/AIChat/wiki)

## Usage

- Log in as Host/SuperUser.
- **In-portal chat**: Open the PersonaBar, select **AI Assistant** in the manage section, configure API key and model in AI Settings, then start chatting.
- **MCP clients**: Open **MCP Settings** in the PersonaBar, generate or paste an API key, (optionally) set validity and enable tools, then use the shown Server URL and client instructions (Goose, Cursor, etc.) to connect your AI client to DNN.

  [See some example prompts](https://github.com/sachatrauwaen/AIChat/wiki/Prompts)

