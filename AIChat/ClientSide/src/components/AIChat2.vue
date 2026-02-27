<template>
    <div > 
    <div v-if="!settings" class="container-fluid pt-5 mt-2" style="padding-bottom: 100px;">
        <div class="fixed-top pt-2 bg-white" style="z-index: 1000;width: 860px;margin-left: 80px;">
            <div class="d-flex gap-2">
                <h2 class="ps-2">AI Assistant</h2>
                
                <small v-if="totalPrice" class="text-muted  position-absolute top-0 mt-2" style="right: 300px;">Total price: ${{totalPrice.toFixed(5)}} ({{totalInputTokens}} input / {{totalOutputTokens}} output tokens)</small>
                
                <button class="btn btn-secondary position-absolute top-0 mt-2" style="right: 80px;" type="button" @click="clear" >
                    New Chat
                </button>
                <button class="btn btn-secondary position-absolute top-0 end-0 me-5 mt-2" type="button" @click="goSettings">
                    <i class="bi bi-gear"></i>
                </button>
            </div>
        </div>
        <div v-for="(msg, i) in messages" :key="i">
            <div v-if="msg.role === 'assistant'">
                <VueMarkdown :source="msg.content" />
            </div>
            <div v-else>
                <div class="alert alert-light" role="alert">
                    <span class="badge text-bg-light">&#128100;</span> {{ msg.content }}
                </div>
            </div>
        </div>
        <div v-if="toolCall">
            <div class="alert alert-light" role="alert">
                <div class="d-flex gap-2">
                    <button class="btn btn-outline-secondary" type="button" @click="toolMore = !toolMore" >                    
                        <span><span class="badge text-bg-dark">Tool</span> {{toolCall.name}}</span>
                    </button>
                    <div v-if="isThinking" class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <div v-else>
                        <button class="btn btn-primary ms-2" type="button" @click="runTool()" >                    
                            <span>Run</span>
                        </button>
                        <button class="btn btn-secondary ms-2" type="button" @click="cancelTool()" >                    
                            <span>Cancel</span>
                        </button>
                    </div>                    
                </div>
                <div v-if="toolMore" class="mt-2">
                    <pre><code class="language-none">{{ toolCall.description }}</code></pre>
                </div>
            </div>
        </div>
        <div v-if="messages.length === 0" class="row mt-5 bg-white p-5" style="z-index: 1000;width: 860px;">
            <h1 class="text-center">How can i help you today ?</h1>
            <h3 class="text-center">I am your DNN AI assistant.</h3>
            <div class="d-flex gap-2 justify-content-center pt-5 mb-5">
                <button class="btn btn-light" @click="userInput='List pages of this website'">List pages of this website</button> 
                <button class="btn btn-light" @click="userInput='Send a email with a joke to info@example.com'">Send a email</button>
                <button class="btn btn-light" @click="userInput='Generate a seo report of the home page in a table'">Make a seo report of the home page</button>
                <button class="btn btn-light" @click="userInput='Show all available tools'">Show tools</button>
            </div>
            <div class="card card-body border-radius-5">
                <div class="d-flex gap-2">
                    <input class="form-control border-0" v-model="userInput" @keyup.enter="sendMessage"  placeholder="Type your message..." />
                    
                </div>
                <div class="d-flex gap-2 pt-2 justify-content-end">
                    
                    <select v-if="rules.length > 0" class="form-select _form-select-sm" style="width: 150px;" v-model="selectedRule">
                        <option value="">No rules</option>
                        <option v-for="r in rules" :key="r"  :value="r">{{r}}</option>
                       
                    </select>
                    <select class="form-select _form-select-sm" style="width: 150px;" v-model="selectedMode">
                        <option value="chat">Chat</option>
                        <option value="readonly">Agent (Read Only)</option>
                        <option value="agent">Agent (Read/Write)</option>
                    </select>
                    <div>
                        <div v-if="isThinking" class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                        <div v-else>
                            <button class="btn btn-primary" @click="sendMessage" :disabled="toolCall">Send</button>
                        </div>
                    </div>
                </div>
                <div v-if="message" class="bg-warning p-2" >
                    {{message}}
                </div>
            </div>
        </div>
        <div v-else class="row position-fixed bottom-0 bg-white p-2 gap-2" style="z-index: 1000;width: 860px;">
            <div class="card card-body">
                <div class="d-flex gap-2">
                    <input class="form-control border-0" v-model="userInput" @keyup.enter="sendMessage"  placeholder="Type your message..." />                                    
                </div>
                <div class="d-flex gap-2 pt-2 justify-content-end">
                    <div class="d-flex gap-2 text-danger mt-1" v-if="selectedMode === 'agent'">
                        <i class="bi bi-exclamation-triangle"></i>
                        <span>In Read/Write mode, modifications can be done on your system.</span>
                    </div>
                    <select v-if="rules.length > 0" class="form-select _form-select-sm" style="width: 150px;" v-model="selectedRule">
                        <option value="">No rules</option>
                        <option v-for="r in rules" :key="r"  :value="r">{{r}}</option>
                       
                    </select>
                    <select class="form-select _form-select-sm w-25" v-model="selectedMode">
                        <option value="chat">Chat</option>
                        <option value="readonly">Agent (Read Only)</option>
                        <option value="agent">Agent (Read/Write)</option>
                    </select>
                    <div>
                        <div v-if="isThinking" class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                        <div v-else>
                            <button class="btn btn-primary" @click="sendMessage" :disabled="toolCall">Send</button>
                        </div>
                    </div>
                </div>
            </div>

            <div v-if="message" class="bg-warning p-2 m-2 col" >
                {{message}}
            </div>
        </div>
    </div>
    <ai-settings ref="settings" v-if="settings" @close="closeSettings" />
    </div>
</template>

<script>
import AIChatService from "../aiChatService";
import VueMarkdown from 'vue-markdown-it';
import Prism from "prismjs";
import AISettings from "./AISettings.vue";

export default {
    mounted() {
        window.Prism = window.Prism || {};
        window.Prism.manual = true;
        this.loadInfo();
    },
    components: {
        VueMarkdown,
        'ai-settings': AISettings
    },
    data() {
        return {
            conversationId: null,
            messages: [],
            userInput: '',
            message: '',
            isThinking: false,
            toolCall: null,
            toolMore: false,            
            selectedMode: 'readonly',
            settings: false,
            rules: [],
            selectedRule: '',
            totalPrice: 0,
            totalInputTokens: 0,
            totalOutputTokens: 0,
        };
    },
    methods: {
        loadInfo() {
            this.isThinking = true;
            AIChatService.getInfo(data => {
                if (data.success) {
                    this.rules = data.rules || [];
                } else {
                    this.message = data.message;
                }
                this.isThinking = false;
            }, this.errorCallback);
        },
        goSettings(){
            this.settings = true;
            this.$nextTick(() => {
                this.$refs.settings.loadSettings();
            });
        },
        closeSettings(){
            this.settings = false;
            this.loadInfo();
        },
        clear() {
            this.conversationId = null;
            this.messages = [];
            this.message = '';
            this.toolCall = null;
            this.toolMore = false;
            this.totalPrice = 0;
            this.totalInputTokens = 0;
            this.totalOutputTokens = 0;
        },
        runTool() {
            if (!this.toolCall) return;
            this.message = '';
            this.toolMore = false;
            this.isThinking = true;

            AIChatService.TornadoChat({
                conversationId: this.conversationId || null,
                message: null,
                runTool: true,
                toolCallId: this.toolCall.id,
                toolName: this.toolCall.name,
                toolArguments: this.toolCall.arguments,
                mode: this.selectedMode,
                rules: this.selectedRule
            }, data => {
                this.isThinking = false;
                if (data.success) {
                    this.message = '';
                    this.conversationId = data.conversationId;
                    this.messages = data.messages || [];
                    this.toolCall = data.toolCall || null;
                    this.totalPrice = data.totalPrice;
                    this.totalInputTokens = data.totalInputTokens;
                    this.totalOutputTokens = data.totalOutputTokens;
                    this.$nextTick(() => { Prism.highlightAll(); });
                } else {
                    this.message = data.message;
                }
            }, this.errorCallback);
        },
        cancelTool() {
            this.toolCall = null;
            this.toolMore = false;
        },
        sendMessage() {
            if (!this.userInput) return;

            this.message = '';
            this.toolMore = false;
            this.isThinking = true;

            AIChatService.TornadoChat({
                conversationId: this.conversationId || null,
                message: this.userInput,
                runTool: false,
                toolCallId: null,
                toolName: null,
                toolArguments: null,
                mode: this.selectedMode,
                rules: this.selectedRule
            }, data => {
                this.isThinking = false;
                if (data.success) {
                    this.message = '';
                    this.userInput = '';
                    this.conversationId = data.conversationId;
                    this.messages = data.messages || [];
                    this.toolCall = data.toolCall || null;
                    this.totalPrice = data.totalPrice;
                    this.totalInputTokens = data.totalInputTokens;
                    this.totalOutputTokens = data.totalOutputTokens;
                    this.$nextTick(() => { Prism.highlightAll(); });
                } else {
                    this.message = data.message;
                }
            }, this.errorCallback);
        },
        errorCallback(error) {
            this.isThinking = false;
            this.message = error && error.message ? error.message : 'An error occurred.';
        }
    }
};
</script>

<style scoped>
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
