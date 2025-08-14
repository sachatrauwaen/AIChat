# DNN PersonaBar AI Chat Extension 

This extension adds an AI-powered chat to the DNN PersonaBar, using a Vue.js frontend and the Anthropic SDK for C#.
You need to set up an Anthropic account and obtain an API key to use this extension.

**Beta version. Make a backup before use.**

#Features

- Chat with your DNN portal.
- There are some tools available to help you manage your DNN portal, like page and module managemnet, send email, get html of an url, read and write files etc.
- Ability to add your own Tools by implemnting : IAITool. Exemple for open content [https://github.com/sachatrauwaen/OpenContentAICHat](https://github.com/sachatrauwaen/OpenContentAICHat)

## Backend (C#)
- Implements a PersonaBar menu and a Web API controller that proxies chat requests to Anthropic.


## Frontend (Vue.js)
- Simple chat UI that calls the backend API.
- Built with Vite

## Usage
- Log in as Host/SuperUser.
- Open the PersonaBar and select "AI Chat".
- Start chatting with Claude via Anthropic!

