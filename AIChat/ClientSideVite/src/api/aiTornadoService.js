function getServiceFramework(controller) {
  const dnn = window.dnn;
  if (!dnn || !dnn.initAIChat) {
    throw new Error("DNN ServiceFramework (initAIChat) is not available.");
  }

  const sf = dnn.initAIChat().utility.sf;
  sf.moduleRoot = "PersonaBar";
  sf.controller = controller;
  return sf;
}

function sfRequest(method, action, data) {
  return new Promise((resolve, reject) => {
    try {
      const sf = getServiceFramework("AITornado");
      const onSuccess = (result) => resolve(result);
      const onError = (err) => {
        console.error(err);
        reject(new Error((err && err.message) || "ServiceFramework request failed"));
      };

      if (method === "GET") {
        sf.get(action, data || {}, onSuccess, onError);
      } else {
        sf.post(action, data || {}, onSuccess, onError);
      }
    } catch (e) {
      reject(e);
    }
  });
}

export function tornadoChat(payload) {
  return sfRequest("POST", "Chat", payload);
}

function getAntiForgeryToken() {
  const el = document.querySelector('input[name="__RequestVerificationToken"]');
  return el ? el.value : "";
}

function getStreamHeaders() {
  const sf = getServiceFramework("AITornado");
  const headers = { "Content-Type": "application/json" };
  const fakeXhr = { setRequestHeader: (key, value) => { headers[key] = value; } };
  sf.setHeaders(fakeXhr);
  return headers;
}

function getServiceBaseUrl() {
  const sf = getServiceFramework("AITornado");
  return sf.getServiceRoot() + "AITornado/";
}

export async function tornadoChatStream(payload, { onToken, onToolCall, onToolStart, onAutoTool, onDone, onError, onWait, signal }) {
  let url;
  try {
    url = getServiceBaseUrl() + "ChatStream";
  } catch {
    url = "/API/PersonaBar/AITornado/ChatStream";
  }

  const resp = await fetch(url, {
    method: "POST",
    headers: getStreamHeaders(),
    body: JSON.stringify(payload),
    signal
  });

  if (!resp.ok) {
    const text = await resp.text().catch(() => "");
    if (onError) onError(`HTTP ${resp.status}: ${text}`);
    return;
  }

  const reader = resp.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const parts = buffer.split("\n\n");
      buffer = parts.pop();

      for (const part of parts) {
        let eventType = "";
        let dataStr = "";
        for (const line of part.split("\n")) {
          if (line.startsWith("event: ")) eventType = line.slice(7);
          else if (line.startsWith("data: ")) dataStr = line.slice(6);
        }
        if (!eventType || !dataStr) continue;

        let data;
        try { data = JSON.parse(dataStr); } catch { continue; }

        switch (eventType) {
          case "delta":
            if (onToken) onToken(data.text);
            break;
          case "tool_start":
            if (onToolStart) onToolStart(data);
            break;
        case "tool_auto":
            if (onAutoTool) onAutoTool(data);
            break;
          case "tool_call":
            if (onToolCall) onToolCall(data);
            break;
          case "done":
            if (onDone) onDone(data);
            break;
          case "wait":
            if (onWait) onWait(data);
            break;
        case "error":
            if (onError) onError(data.message);
            break;
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}

export function getInfo() {
  return sfRequest("GET", "GetInfo");
}

export function getSettings() {
  return sfRequest("GET", "GetSettings");
}

export function saveSettings(settings) {
  return sfRequest("POST", "SaveSettings", settings);
}

export function saveChatPreferences(preferences) {
  return sfRequest("POST", "SaveChatPreferences", preferences);
}

export function getConversations() {
  return sfRequest("GET", "GetConversations");
}

export function loadConversation(id) {
  return sfRequest("GET", "LoadConversation", { id });
}

export function deleteConversation(id) {
  return sfRequest("POST", "DeleteConversation", { id });
}
