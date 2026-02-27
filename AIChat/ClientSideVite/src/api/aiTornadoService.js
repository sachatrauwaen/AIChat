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

export function getInfo() {
  return sfRequest("GET", "GetInfo");
}

export function getSettings() {
  return sfRequest("GET", "GetSettings");
}

export function saveSettings(settings) {
  return sfRequest("POST", "SaveSettings", settings);
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
