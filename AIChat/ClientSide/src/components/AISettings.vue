<template>
    <div class="container-fluid pt-5 mt-2" style="padding-bottom: 90px;">
        <div class="fixed-top pt-2 bg-white" style="z-index: 1000;width: 860px;margin-left: 80px;">
            <div class="d-flex gap-2">
                <h2 class="ps-2">AI Settings</h2>
            </div>
        </div>
        <div>
            <!-- API Key Input -->
            <div class="mb-4">
                <label for="apiKey" class="form-label">API Key</label>
                <input 
                    type="password" 
                    class="form-control" 
                    id="apiKey" 
                    v-model="apiKey" 
                    placeholder="Enter your API key">
                    <a href="https://console.anthropic.com" target="_blank">Get Anthropic API key</a>
            </div>
            <div class="mb-4">                
                <label for="maxTokens" class="form-label">Max Tokens to Generate</label>
                <input 
                    type="number" 
                    class="form-control" 
                    id="maxTokens" 
                    v-model="maxTokens" 
                    >
            </div>
            <div class="mb-4">
                <label for="model" class="form-label">Model</label>
                <select 
                    class="form-control" 
                    id="model" 
                    v-model="model" 
                    >
                    <option v-for="model in models" :key="model.value" :value="model.value">{{model.name}}</option>
                </select>
            </div>
            <div class="mb-4">
                <label class="form-label">Activated tools</label>
                <div v-for="tool in tools" :key="tool.name" class="mb-2">
                    <input type="checkbox" v-model="tool.active" :id="tool.name">
                    <label :for="tool.name" class="ps-2">{{tool.name}} : {{tool.description}}</label>
                </div>
            </div>
            <div class="mb-4">
                <label class="form-label">Automatic execution of tools</label>
                <div class="mb-2">
                    <input type="checkbox" v-model="autoReadonlyTools" :id="autoReadonlyTools">
                    <label :for="autoReadonlyTools" class="ps-2">Readonly tools</label>
                </div>
                <div class="mb-2">
                    <input type="checkbox" v-model="autoWriteTools" :id="autoWriteTools">
                    <label :for="autoWriteTools" class="ps-2">Write tools</label>
                </div>
            </div>
            <div class="mb-4">
                <label for="globalRules" class="form-label">Global instructions</label>
                <textarea 
                            class="form-control" 
                            v-model="globalRules" 
                            rows="5"
                            placeholder="Enter your instructions"
                             style="min-height: 150px; overflow-y: scroll;"></textarea>
            </div>
            <!-- Rules Management -->
            <div class="mb-4">
                <label class="form-label">Specific rules</label><br />
                <div v-for="(rule, index) in rules" :key="index" class="mb-3">
                    <div class="d-flex gap-2 mb-2">
                        <input type="text" class="form-control" v-model="rule.name" 
                        placeholder="Rule name" 
                        pattern="^[^\\/:*?&quot;<>|]+$" 
                        title="Please enter a valid file name without special characters">
                        <button 
                            class="btn btn-danger"
                            @click="deleteRule(index)">
                            <i class="bi bi-trash"></i>
                            Delete
                        </button>
                    </div>
                    <div>
                        <textarea 
                            class="form-control" 
                            v-model="rule.rule" 
                            rows="5"
                            placeholder="Enter your instructions"
                           
                            style="min-height: 150px; overflow-y: scroll;"></textarea>
                    </div>
                    <hr />
                </div>
                <button 
                    class="btn btn-secondary mt-2"
                    @click="addRule">
                    <i class="bi bi-plus-circle"></i>
                    Add New Rule
                </button>
            </div>          
        </div>
        <div class="row position-fixed bottom-0 bg-white p-2 gap-2" style="z-index: 1000;width: 860px;">
            <div class="">
                <div class="d-flex gap-2 pt-2 justify-content-end">
                    <button class="btn btn-secondary" @click="cancel">Cancel</button>
                    <div>                       
                        <div v-if="isThinking" class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                        <div v-else>
                            <button class="btn btn-primary" @click="save">save</button>
                        </div>
                    </div>
                </div>
            </div>

            <div v-if="message" class="bg-warning p-2 m-2 col" >
                {{message}}
            </div>
        </div>
    </div>
</template>

<script>
import AIChatService from "../aiChatService";


export default {
    mounted() {
        // Load initial data
        //this.loadSettings();
    },
    components: {
        
    },
    data() {
        return {
            apiKey: '',
            rules: [],
            message: '',
            isThinking: false,
           globalRules: '',
           models: [],
           model: '',
           tools: [],
           autoReadonlyTools: false,
           autoWriteTools: false
        };
    },
    methods: {
        addRule() {
            this.rules.push({name: '', rule: ''});
        },
        deleteRule(index) {
            this.rules.splice(index, 1);
        },
        loadSettings() {
            this.isThinking = true;
            AIChatService.getSettings(data => {
                if (data.success) {
                    this.apiKey = data.apiKey || '';
                    this.model = data.model || '';
                    this.models = data.models || [];
                    this.rules = data.rules || [];
                    this.globalRules= data.globalRules || '';
                    this.tools = data.tools || [];
                    this.maxTokens = data.maxTokens || 1024;
                    this.autoReadonlyTools = data.autoReadonlyTools || false;
                    this.autoWriteTools = data.autoWriteTools || false;
                } else {
                    this.message = data.message;
                }
                this.isThinking = false;
            }, this.errorCallback);
        },
        save() {
            this.isThinking = true;
            AIChatService.saveSettings({
                apiKey: this.apiKey,
                model: this.model,
                rules: this.rules,
                globalRules: this.globalRules,
                tools: this.tools,
                maxTokens: this.maxTokens,
                autoReadonlyTools: this.autoReadonlyTools,
                autoWriteTools: this.autoWriteTools
            }, data => {
                this.isThinking = false;
                if (data.success) {
                    this.message = 'Settings saved successfully';                    
                } else {
                    this.message = data.message;
                }
                this.$emit('close');
            }, this.errorCallback);
        },
        cancel(){
            this.$emit('close');
        },
        errorCallback(error) {
            this.isThinking = false;
            this.message = error && error.message ? error.message : 'An error occurred.';
        }
    }
};
</script>

<style scoped>
/* Basic styling for the markdown content */
:deep(.markdown-body) {
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
    line-height: 1.6;
}

:deep(h1), :deep(h2), :deep(h3), :deep(h4), :deep(h5), :deep(h6) {
    
    margin-bottom: 16px;
    
    line-height: 1.25;
}

:deep(h1) {
    font-size: 2em;
    font-weight: 600;
    
}

:deep(h2) {
    font-size: 1.5em;
    
    
}

:deep(pre) {
    background-color: #f6f8fa;
    padding: 16px;
    border-radius: 6px;
    overflow: auto;
}

:deep(code) {
    background-color: rgba(27, 31, 35, 0.05);
    border-radius: 3px;
    padding: 0.2em 0.4em;
    font-family: SFMono-Regular, Consolas, "Liberation Mono", Menlo, monospace;
}

:deep(table) {
    border-collapse: collapse;
    width: 100%;
    margin: 16px 0;
}

:deep(th), :deep(td) {
    border: 1px solid #dfe2e5;
    padding: 6px 13px;
}

:deep(tr:nth-child(2n)) {
    background-color: #f6f8fa;
}

:deep(blockquote) {
    border-left: 4px solid #dfe2e5;
    color: #6a737d;
    margin: 0;
    padding: 0 16px;
}
</style> 