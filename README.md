# DNN AI Assistant 

This extension adds an AI-powered chat to the DNN PersonaBar, using a Vue.js frontend and the Anthropic SDK for C#.
You need to set up an Anthropic account and [obtain an API key](https://console.anthropic.com) to use this extension.

**Beta version. Make a backup before use.**

---

<img width="467" height="230" alt="image" src="https://github.com/user-attachments/assets/be0346a9-12fc-47f0-b318-4aa356979bf5" />



## Features
- Chat with your DNN portal.
- There are some tools available to help you manage your DNN portal, like page and module management, send email, get the HTML of a url, read and write files etc.
- Ability to add your own Tools by implementing : IAITool. Example for Open Content [https://github.com/sachatrauwaen/OpenContentAICHat](https://github.com/sachatrauwaen/OpenContentAICHat)

## Settings

<img width="372" height="460" alt="image" src="https://github.com/user-attachments/assets/95c2b4ad-ea0a-486b-a991-e5e8588bee36" />

---

## Backend (C#)
- Implements a PersonaBar menu and a Web API controller that proxies chat requests to Anthropic.

## Frontend (Vue.js)
- Simple chat UI that calls the backend API.
- Built with Vite

## Usage
- Log in as Host/SuperUser.
- Open the PersonaBar and select "AI Assistant" in manage section.
- Start chatting with Claude via Anthropic!

