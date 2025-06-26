//import utils from "utils";

class AIChatService {
    getServiceFramework(controller) {
        let sf = window.dnn.initAIChat().utility.sf;

        sf.moduleRoot = "PersonaBar";
        sf.controller = controller;
        return sf;
    }

    getInfo(callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.get("GetInfo", {}, callback, errorCallback);
    }

    getSettings(callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.get("GetSettings", {}, callback, errorCallback);
    }

    saveSettings(settings, callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.post("SaveSettings", settings, callback, errorCallback);
    }

    Chat(message, callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.post("Chat", message, callback, errorCallback);
    }
    ChatWithTools(message, callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.post("ChatWithTools", message, callback, errorCallback);
    }
   
}

const aiChatService = new AIChatService();
export default aiChatService;