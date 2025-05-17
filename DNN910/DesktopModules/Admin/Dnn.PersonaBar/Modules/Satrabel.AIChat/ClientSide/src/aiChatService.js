//import utils from "utils";

class AIChatService {
    getServiceFramework(controller) {
        let sf = window.dnn.initAIChat().utility.sf;

        sf.moduleRoot = "PersonaBar";
        sf.controller = controller;
        return sf;
    }

    getSettings(callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.get("GetSettings", {}, callback, errorCallback);
    }

    Chat(message, callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.post("Chat", message, callback, errorCallback);
    }
    ChatWithTools(message, callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.post("ChatWithTools", message, callback, errorCallback);
    }
    ChatWithTools2(message, callback, errorCallback) {
        const sf = this.getServiceFramework("AIChat");
        sf.post("ChatWithTools2", message, callback, errorCallback);
    }
}

const aiChatService = new AIChatService();
export default aiChatService;