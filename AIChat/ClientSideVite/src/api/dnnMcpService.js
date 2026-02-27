//import utils from "utils";

class DnnMcpService {
    getServiceFramework(controller) {
        const sf = dnn.initAIChat().utility.sf;

        sf.moduleRoot = "PersonaBar";
        sf.controller = controller;
        return sf;
    }

    getInfo(callback, errorCallback) {
        const sf = this.getServiceFramework("DnnMcp");
        sf.get("GetInfo", {}, callback, errorCallback);
    }

    getSettings(callback, errorCallback) {
        const sf = this.getServiceFramework("DnnMcp");
        sf.get("GetSettings", {}, callback, errorCallback);
    }

    saveSettings(settings, callback, errorCallback) {
        const sf = this.getServiceFramework("DnnMcp");
        sf.post("SaveSettings", settings, callback, errorCallback);
    }

  
}

const dnnMcpService = new DnnMcpService();
export default dnnMcpService;