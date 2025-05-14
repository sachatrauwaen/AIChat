<template>
    <div class="container-fluid pb-5">
        <h2>AI Chat</h2>
        <div v-if="messages.length === 0" class="alert alert-info" role="alert">
            I am your assistant and i can help you with your questions about this website.
            I have access to the pages of this website.       
        </div>
        <div v-for="(msg, i) in messages" :key="i">
            <div v-if="msg.contentType === 'tool_result'">
                <p>
                <button class="btn btn-secondary" type="button" @click="toggleVisible(i)" aria-expanded="false" aria-controls="collapseWidthExample">
                    
                    <span><span class="badge text-bg-light">Tool</span> {{msg.toolName}}</span>
                </button>
                </p>
                <div style="min-height: 120px;" v-if="visibleMessages.includes(i)">                    
                        <div class="card card-body" >
                            <div>
                                <pre><code class="language-none" v-html="msg.toolFullname" ></code></pre>                                
                            </div>
                            <hr />
                            <div>
                                <pre><code class="language-none" v-html="msg.content" ></code></pre>
                            </div>
                        </div>                    
                </div>  
            </div>                               
            <div v-else-if="msg.role === 'assistant'">
                <VueMarkdown :source="msg.content" />
            </div>
            <div v-else>
                <div class="alert alert-light" role="alert">
                    <span class="badge text-bg-warning">&#128100;</span> {{ msg.content }}
                </div>
            </div>
        </div>
        <div class="row position-fixed bottom-0 bg-white pt-2 pb-2" style="z-index: 1000;width: 860px;">
        <div class="d-flex gap-2">
            <input class="form-control" v-model="userInput" @keyup.enter="sendMessage" placeholder="Type your message..." />
            
            <div v-if="isThinking" class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <div v-else>
                <button class="btn btn-primary" @click="sendMessage">Send</button>
            </div>
        </div>
        <div>
            {{message}}
        </div>
        </div>
    </div>
</template>

<script>
import AIChatService from "../aiChatService";
import VueMarkdown from 'vue-markdown-it';
import Prism from "prismjs";

export default {
    mounted() {
        window.Prism = window.Prism || {};
        window.Prism.manual = true;
        
    },
    components: {
        VueMarkdown
    },
    data() {
        return {
            messages: [],
            userInput: '',
            message: '',
            isThinking: false,
            visibleMessages: []
        };
    },
    methods: {
        toggleVisible(i) {
            if (this.visibleMessages.includes(i)) {
                this.visibleMessages = this.visibleMessages.filter(m => m !== i);
            } else {
                this.visibleMessages.push(i);
                this.$nextTick(() => {
                        Prism.highlightAll();
                    });
            }
        },
        sendMessage() {
            if (!this.userInput) return;
            this.messages.push({ role: 'user', content: this.userInput });
            this.message = '';
            this.isThinking = true;

            AIChatService.ChatWithTools({ messages : this.messages }, data => {
                this.isThinking = false;
                if (data.success) {
                    this.message = '';
                    
                    this.messages = data.messages;
                    this.userInput = '';
                    this.$nextTick(() => {
                        Prism.highlightAll();
                    });
                } else {
                    this.message = data.message;
                }
            }, this.errorCallback);
        },
        errorCallback(error) {
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
    margin-top: 24px;
    margin-bottom: 16px;
    font-weight: 600;
    line-height: 1.25;
}

:deep(h1) {
    font-size: 2em;
    border-bottom: 1px solid #eaecef;
    padding-bottom: 0.3em;
}

:deep(h2) {
    font-size: 1.5em;
    border-bottom: 1px solid #eaecef;
    padding-bottom: 0.3em;
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