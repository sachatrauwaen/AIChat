<template>
    <div class="mcp-settings" style="padding-bottom: 90px;">
        <div class="mcp-settings__header" style="z-index: 1000;width: 814px;margin-left: 80px;">
            <div class="mcp-settings__header-row">
                <h2 class="mcp-settings__title">MCP Settings</h2>
            </div>
        </div>
        <div class="mcp-settings__main">
            <!-- API Key Input -->
            <div class="mcp-settings__section">
                <label for="apiKey" class="mcp-settings__label">API Key (Bearer Token Authentication)</label>
                <div class="mcp-settings__field mcp-settings__field--inline">
                    <input 
                        class="mcp-settings__input" 
                        id="apiKey" 
                        v-model="apiKey" 
                        placeholder="Enter your API key">
                    <button 
                        class="mcp-settings__button mcp-settings__button--secondary"
                        @click="copyToClipboard(apiKey, 'API Key copied!')"
                        :disabled="!apiKey || isThinking">
                        Copy
                    </button>
                    <button 
                        class="mcp-settings__button mcp-settings__button--primary"
                        @click="generateApiKey"
                        :disabled="isThinking">
                        Generate
                    </button>
                </div>
                <div class="mcp-settings__field mcp-settings__field--inline">
                    <input type="checkbox" v-model="apiKeyValidDelayActive" id="apiKeyValidDelayActive">
                    <label for="apiKeyValidDelayActive" class="mcp-settings__label">Active</label>
                </div>
                <div class="mcp-settings__field mcp-settings__field--inline">                    
                    <label for="apiKeyValidDelay" class="mcp-settings__label">API Key Valid Delay</label>
                </div>
                <div class="mcp-settings__field mcp-settings__field--inline">
                    <select 
                        class="mcp-settings__input" 
                        id="apiKeyValidDelay" 
                        v-model="apiKeyValidDelay">
                         
                        <option value="1">1 day</option>
                        <option value="7">7 days</option>
                        <option value="30">30 days</option>
                        <option value="60">60 days</option>
                        <option value="90">90 days</option>
                        <option value="180">180 days</option>
                        <option value="365">1 year</option>
                        <option value="0">Never expires</option>
                    </select>
                    <button 
                        class="mcp-settings__button mcp-settings__button--success"
                        @click="extendDelay"
                        :disabled="isThinking">
                        Extend
                    </button>
                </div>
                <small 
                    :class="[
                        'mcp-settings__helper-text',
                        isApiKeyExpired ? 'mcp-settings__helper-text--danger' : 'mcp-settings__helper-text--success'
                    ]">
                    Valid until: {{ validUntilDateFormatted }}
                </small>
            </div>

            <!-- Toggle button for MCP Configuration -->
            <div class="mcp-settings__section" v-if="apiKey">
                <button 
                    class="mcp-settings__button mcp-settings__button--secondary"
                    @click="showMcpConfig = !showMcpConfig">
                    {{ showMcpConfig ? 'Hide' : 'Show' }} MCP Server Configuration
                </button>
                
                <div class="mcp-settings__config" v-if="apiKey && showMcpConfig">
                    <h5 class="mcp-settings__section-title">MCP Server Configuration</h5>
                    <p class="mcp-settings__tip">For beginners start with <a href="https://block.github.io/goose/" target="_blank" rel="noopener noreferrer" class="mcp-settings__tip-link">Goose</a>.</p>
                    
                    <div class="mcp-settings__field">
                        <label class="mcp-settings__label">Server URL:</label>
                        <div class="mcp-settings__input-group">
                            <input 
                                type="text" 
                                class="mcp-settings__input" 
                                :value="mcpServerUrl" 
                                readonly
                                ref="serverUrlInput">
                            <button 
                            class="mcp-settings__button mcp-settings__button--secondary" 
                                @click="copyToClipboard(mcpServerUrl, 'Server URL copied!')">
                                Copy
                            </button>
                        </div>
                    </div>

                    <!-- Client-specific configurations -->
                    <div class="mcp-settings__field">
                        <label class="mcp-settings__label">Client Configuration:</label>
                        
                        <!-- Tabs for different clients -->
                        <ul class="mcp-settings__tabs" role="tablist">
                            <li class="mcp-settings__tab-item" role="presentation">
                                <button 
                                    :class="['mcp-settings__tab-link', { 'mcp-settings__tab-link--active': activeTab === 'goose' }]"
                                    @click="activeTab = 'goose'"
                                    type="button" 
                                    role="tab">
                                    Goose
                                </button>
                            </li>
                            <li class="mcp-settings__tab-item" role="presentation">
                                <button 
                                    :class="['mcp-settings__tab-link', { 'mcp-settings__tab-link--active': activeTab === 'cursor' }]"
                                    @click="activeTab = 'cursor'"
                                    type="button" 
                                    role="tab">
                                    Cursor
                                </button>
                            </li>
                            <li class="mcp-settings__tab-item" role="presentation">
                                <button 
                                    :class="['mcp-settings__tab-link', { 'mcp-settings__tab-link--active': activeTab === 'copilot' }]"
                                    @click="activeTab = 'copilot'"
                                    type="button" 
                                    role="tab">
                                    GitHub Copilot
                                </button>
                            </li>
                            <li class="mcp-settings__tab-item" role="presentation">
                                <button 
                                    :class="['mcp-settings__tab-link', { 'mcp-settings__tab-link--active': activeTab === 'claude' }]"
                                    @click="activeTab = 'claude'"
                                    type="button" 
                                    role="tab">
                                    Claude Desktop
                                </button>
                            </li>
                        </ul>
            
                        <!-- Tab content -->
                        <div class="mcp-settings__tab-content">
                            <!-- Goose -->
                            <div v-show="activeTab === 'goose'" class="mcp-settings__tab-pane" role="tabpanel">
                                                             
                                <div class="mcp-settings__field mcp-settings__field--full-width">
                                    <a 
                                        :href="gooseExtensionUrl" 
                                        class="mcp-settings__button mcp-settings__button--primary mcp-settings__goose-add-link">
                                        Add DNN MCP server in Goose
                                    </a>
                                    <p></p>
                                    <p class="mcp-settings__text" style="margin-top: 12px;">
                                        Then add the authorization header to the Goose extension
                                    </p>
                                    <div class="mcp-settings__field">
                                        <label class="mcp-settings__label">Header name</label>
                                        <div class="mcp-settings__input-group">
                                            <input 
                                                type="text" 
                                                class="mcp-settings__input" 
                                                value="Authorization" 
                                                readonly
                                                style="font-size: 11px;">
                                            <button 
                                                class="mcp-settings__button mcp-settings__button--secondary" 
                                                @click="copyToClipboard('Authorization', 'Header name copied!')">
                                                Copy
                                            </button>
                                        </div>
                                    </div>
                                    <div class="mcp-settings__field">
                                        <label class="mcp-settings__label">Header value</label>
                                        <div class="mcp-settings__input-group">
                                            <input 
                                                type="text" 
                                                class="mcp-settings__input" 
                                                :value="authorizationHeader" 
                                                readonly
                                                style="font-size: 11px;">
                                            <button 
                                                class="mcp-settings__button mcp-settings__button--secondary" 
                                                @click="copyToClipboard(authorizationHeader, 'Header value copied!')">
                                                Copy
                                            </button>
                                        </div>
                                    </div>
                                  
                                </div>
                            </div>

                            <!-- Cursor -->
                            <div v-show="activeTab === 'cursor'" class="mcp-settings__tab-pane" role="tabpanel">
                                <p class="mcp-settings__text">
                                    Add this to your Cursor MCP settings file:<br>
                                    <strong>Windows:</strong> <code>%APPDATA%\Cursor\User\globalStorage\mcp.json</code><br>
                                    <strong>macOS:</strong> <code>~/Library/Application Support/Cursor/User/globalStorage/mcp.json</code>
                                </p>
                                <div class="mcp-settings__code-block-wrapper">
                                    <pre class="mcp-settings__code-block" style="max-height: 300px; overflow-y: auto;"><code>{{ cursorConfigJson }}</code></pre>
                                    <button 
                                        class="mcp-settings__button mcp-settings__button--copy" 
                                        @click="copyToClipboard(cursorConfigJson, 'Cursor configuration copied!')">
                                        Copy JSON
                                    </button>
                                </div>
                            </div>
            
                            <!-- GitHub Copilot -->
                            <div v-show="activeTab === 'copilot'" class="mcp-settings__tab-pane" role="tabpanel">
                                <p class="mcp-settings__text">
                                    Add this to your VS Code settings.json for GitHub Copilot MCP integration:<br>
                                    <strong>Settings location:</strong> Open Command Palette (Ctrl+Shift+P / Cmd+Shift+P) → "Preferences: Open Settings (JSON)"
                                </p>
                                <div class="mcp-settings__code-block-wrapper">
                                    <pre class="mcp-settings__code-block" style="max-height: 300px; overflow-y: auto;"><code>{{ copilotConfigJson }}</code></pre>
                                    <button 
                                        class="mcp-settings__button mcp-settings__button--copy" 
                                        @click="copyToClipboard(copilotConfigJson, 'GitHub Copilot configuration copied!')">
                                        Copy JSON
                                    </button>
                                </div>
                            </div>

                            <!-- Claude Desktop -->
                            <div v-show="activeTab === 'claude'" class="mcp-settings__tab-pane" role="tabpanel">
                                <p class="mcp-settings__text">
                                    Add this to your Claude Desktop configuration file:<br>
                                    <strong>Windows:</strong> <code>%APPDATA%\Claude\claude_desktop_config.json</code><br>
                                    <strong>macOS:</strong> <code>~/Library/Application Support/Claude/claude_desktop_config.json</code><br>
                                    <strong>Requirements:</strong> npx and mcp-remote must be installed.
                                </p>
                                <div class="mcp-settings__code-block-wrapper">
                                    <pre class="mcp-settings__code-block" style="max-height: 300px; overflow-y: auto;"><code>{{ claudeConfigJson }}</code></pre>
                                    <button 
                                        class="mcp-settings__button mcp-settings__button--copy" 
                                        @click="copyToClipboard(claudeConfigJson, 'Claude configuration copied!')">
                                        Copy JSON
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
 
            <div class="mcp-settings__section">
                <label class="mcp-settings__label">Activated tools</label>
                <template v-for="(categoryTools, category) in toolsByCategory" :key="category">
                    <div class="mcp-settings__tool-category" v-if="categoryTools.length">
                        <div class="mcp-settings__tool-category-header">
                            <h4 class="mcp-settings__tool-category-title">{{ category || 'Other' }}</h4>
                            <div class="mcp-settings__tool-category-actions">
                                <button
                                    type="button"
                                    class="mcp-settings__category-btn"
                                    @click="selectAllInCategory(categoryTools)"
                                >
                                    Select all
                                </button>
                                <button
                                    type="button"
                                    class="mcp-settings__category-btn"
                                    @click="deselectAllInCategory(categoryTools)"
                                >
                                    Deselect all
                                </button>
                            </div>
                        </div>
                        <div v-for="tool in categoryTools" :key="tool.name" class="mcp-settings__checkbox-row">
                            <input type="checkbox" v-model="tool.active" :id="tool.name" class="mcp-settings__checkbox">
                            <label :for="tool.name" class="mcp-settings__checkbox-label">{{ tool.name }} : {{ tool.description }}</label>
                        </div>
                    </div>
                </template>
            </div>
            <!-- Rules Management -->
            <div class="mcp-settings__section">
                <label class="mcp-settings__label">Prompts</label><br />
                <div v-for="(rule, index) in rules" :key="index" class="mcp-settings__rule">
                    <div class="mcp-settings__rule-header">
                        <input type="text" class="mcp-settings__input" v-model="rule.name" 
                        placeholder="Prompt name" 
                        pattern="^[^\\/:*?&quot;<>|]+$" 
                        title="Please enter a valid file name without special characters">
                        <button 
                            class="mcp-settings__button mcp-settings__button--danger"
                            @click="deleteRule(index)">
                            Delete
                        </button>
                    </div>
                    <div>
                        <textarea 
                            class="mcp-settings__textarea" 
                            v-model="rule.rule" 
                            rows="5"
                            placeholder="Enter your instructions"
                           
                            style="min-height: 150px; overflow-y: scroll;"></textarea>
                    </div>
                </div>
                <button 
                    class="mcp-settings__button mcp-settings__button--secondary"
                    @click="addRule">
                    + Add New Prompt
                </button>
            </div>          
        </div>
        <div class="mcp-settings__footer" style="z-index: 1000;">
            
            <div class="mcp-settings__footer-right">
                <div v-if="message" class="mcp-settings__message">
                {{ message }}
            </div>
                <button class="mcp-settings__button mcp-settings__button--secondary" @click="cancel">Cancel</button>
                <div>                       
                    <div v-if="isThinking" class="mcp-settings__spinner" role="status">
                        <span class="mcp-settings__spinner-text">Loading...</span>
                    </div>
                    <div v-else>
                        <button class="mcp-settings__button mcp-settings__button--primary" @click="save">save</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import DnnMcpService from "../api/dnnMcpService";

export default {
    name: 'AISettings',
    mounted() {
        // Load initial data
        this.loadSettings();
    },
    data() {
        return {
            apiKey: '',
            apiKeyValidDelay: 90,
            apiKeyValidDelayActive: true,
            apiKeyValidUntilDate: '',
            rules: [],
            message: '',
            isThinking: false,
            tools: [],
            activeTab: 'goose',
            showMcpConfig: false
        };
    },
    computed: {
        validUntilDateFormatted() {
            if (!this.apiKeyValidUntilDate || parseInt(this.apiKeyValidDelay) === 0) {
                return 'Never expires';
            }
            const validUntil = new Date(this.apiKeyValidUntilDate);
            
            // Format the date as: Month Day, Year
            const options = { year: 'numeric', month: 'long', day: 'numeric' };
            return validUntil.toLocaleDateString('en-US', options);
        },
        isApiKeyExpired() {
            if (!this.apiKeyValidUntilDate || parseInt(this.apiKeyValidDelay) === 0) {
                return false;
            }
            const validUntil = new Date(this.apiKeyValidUntilDate);
            return new Date() > validUntil;
        },
        mcpServerUrl() {
            const baseUrl = window.location.origin + DnnMcpService.getServiceFramework().getSiteRoot();
            return `${baseUrl}API/Dnn/McpWebApi/Mcp`;
        },
        authorizationHeader() {
            return `Bearer ${this.apiKey}`;
        },
        claudeConfigJson() {
            const config = {
                "mcpServers": {
                    "dnn-mcp": {
                        "command": "npx",
                        "args": [
                            "mcp-remote",
                            this.mcpServerUrl,
                            ...(this.mcpServerUrl.startsWith('https') ? [] : ["--allow-http"]),
                            "--header",                            
                            `Authorization:\${AUTH_HEADER}`                         
                        ],
                        "env": {
                            "AUTH_HEADER": this.authorizationHeader
                        }
                    }
                }
            };
            return JSON.stringify(config, null, 2);
        },
        cursorConfigJson() {
            const config = {
                "mcpServers": {
                    "dnn-mcp": {                      
                        "url": this.mcpServerUrl,
                        "headers": {
                            "Authorization": this.authorizationHeader
                        }                        
                    }
                }
            };
            return JSON.stringify(config, null, 2);
        },
        copilotConfigJson() {
            const config = {
                "github.copilot.chat.mcp.servers": {
                    "dnn-mcp": {
                        "url": this.mcpServerUrl,
                        "type": "http",
                        "headers": {
                            "Authorization": this.authorizationHeader
                        }
                    }
                }
            };
            return JSON.stringify(config, null, 2);
        },
        gooseConfigJson() {
            const config = {
                "mcpServers": {
                    "dnn-mcp": {
                        "url": this.mcpServerUrl,
                        "headers": {
                            "Authorization": this.authorizationHeader
                        }
                    }
                }
            };
            return JSON.stringify(config, null, 2);
        },
        gooseExtensionUrl() {
            const params = new URLSearchParams({
                url: this.mcpServerUrl,
                type: 'streamable_http',
                id: 'dnn-mcp',
                name: 'DNN MCP',
                description: 'DNN MCP server - tools for this site'
            });
            return `goose://extension?${params.toString()}`;
        },
        toolsByCategory() {
            const groups = {};
            for (const tool of this.tools || []) {
                const cat = (tool.category && String(tool.category).trim()) || '';
                if (!groups[cat]) groups[cat] = [];
                groups[cat].push(tool);
            }
            const ordered = {};
            const keys = Object.keys(groups).sort((a, b) => {
                if (a === '') return 1;
                if (b === '') return -1;
                return a.localeCompare(b);
            });
            keys.forEach(k => { ordered[k] = groups[k]; });
            return ordered;
        }
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
            DnnMcpService.getSettings(data => {
                if (data.success) {
                    this.apiKey = data.apiKey || '';
                    this.apiKeyValidDelay = data.apiKeyValidDelay || 90;
                    this.apiKeyValidDelayActive = data.apiKeyValidDelayActive !== undefined ? data.apiKeyValidDelayActive : true;
                    this.apiKeyValidUntilDate = data.apiKeyValidUntilDate || '';
                    this.rules = data.rules || [];
                    this.tools = data.tools || [];
                } else {
                    this.message = data.message;
                }
                this.isThinking = false;
            }, this.errorCallback);
        },
        save() {
            this.isThinking = true;
            
            DnnMcpService.saveSettings({
                apiKey: this.apiKey,
                apiKeyValidDelay: this.apiKeyValidDelay,
                apiKeyValidDelayActive: this.apiKeyValidDelayActive,
                apiKeyValidUntilDate: this.apiKeyValidUntilDate,
                rules: this.rules,
                tools: this.tools
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
        selectAllInCategory(categoryTools) {
            categoryTools.forEach(t => { t.active = true; });
        },
        deselectAllInCategory(categoryTools) {
            categoryTools.forEach(t => { t.active = false; });
        },
        generateApiKey() {
            // Generate a random API key (UUID-like format)
            const crypto = window.crypto || window.msCrypto;
            const array = new Uint8Array(32);
            crypto.getRandomValues(array);
            this.apiKey = Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
            
            // Calculate and set the valid until date
            this.calculateValidUntilDate();
            
            // Show MCP configuration automatically
            this.showMcpConfig = true;
            
            this.message = 'API Key generated successfully. Remember to save your settings.';
        },
        extendDelay() {
            // Extend the delay by the current selected value
            const currentDelay = parseInt(this.apiKeyValidDelay);
            if (currentDelay === 0) {
                this.message = 'API Key is already set to never expire.';
                return;
            }
            
            // Calculate the new valid until date by extending from current date
            this.calculateValidUntilDate();
            
            this.message = `API Key delay extended to ${this.apiKeyValidDelay} days. Remember to save your settings.`;
        },
        calculateValidUntilDate() {
            // Calculate the valid until date based on the current delay
            if (parseInt(this.apiKeyValidDelay) > 0) {
                const today = new Date();
                const validUntil = new Date(today);
                validUntil.setDate(today.getDate() + parseInt(this.apiKeyValidDelay));
                this.apiKeyValidUntilDate = validUntil.toISOString();
            } else {
                this.apiKeyValidUntilDate = '';
            }
        },
        errorCallback(error) {
            this.isThinking = false;
            this.message = error && error.message ? error.message : 'An error occurred.';
        },
        copyToClipboard(text, successMessage) {
            // Use the modern Clipboard API
            if (navigator.clipboard && window.isSecureContext) {
                navigator.clipboard.writeText(text).then(() => {
                    this.message = successMessage || 'Copied to clipboard!';
                }).catch(err => {
                    this.message = 'Failed to copy to clipboard';
                    console.error('Failed to copy:', err);
                });
            } else {
                // Fallback for older browsers
                const textArea = document.createElement('textarea');
                textArea.value = text;
                textArea.style.position = 'fixed';
                textArea.style.left = '-999999px';
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();
                try {
                    document.execCommand('copy');
                    this.message = successMessage || 'Copied to clipboard!';
                } catch (err) {
                    this.message = 'Failed to copy to clipboard';
                    console.error('Failed to copy:', err);
                }
                document.body.removeChild(textArea);
            }
        }
    }
};
</script>

<style scoped>
.mcp-settings {
    max-width: 900px;
    margin: 0 auto;
    padding: 96px 24px 80px;
    box-sizing: border-box;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
}

.mcp-settings__header {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    background: #ffffff;
    padding: 12px 24px;
    border-bottom: 1px solid #e0e0e0;
}

.mcp-settings__header-row {
    display: flex;
    align-items: center;
    gap: 8px;
}

.mcp-settings__title {
    margin: 0;
    font-size: 22px;
    font-weight: 600;
}

.mcp-settings__main {
    display: flex;
    flex-direction: column;
    gap: 24px;
}

.mcp-settings__section {
    padding: 16px 20px;
    border-radius: 8px;
    background: #fafafa;
    border: 1px solid #e0e0e0;
}

.mcp-settings__config {
    margin-top: 16px;
}

.mcp-settings__section-title {
    margin: 0 0 12px;
    font-size: 16px;
    font-weight: 600;
}

.mcp-settings__tip {
    margin: 0 0 12px;
    padding: 8px 12px;
    font-size: 13px;
    color: #2e7d32;
    background: #e8f5e9;
    border-radius: 4px;
    border-left: 3px solid #2e7d32;
}

.mcp-settings__tip-link {
    color: #1b5e20;
    font-weight: 600;
    text-decoration: none;
}

.mcp-settings__tip-link:hover {
    text-decoration: underline;
}

.mcp-settings__field {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-bottom: 12px;
}

.mcp-settings__field--inline {
    flex-direction: row;
    align-items: center;
}

.mcp-settings__field--inline .mcp-settings__input {
    flex: 1 1 auto;
    min-width: 0;
}

.mcp-settings__field--inline .mcp-settings__button {
    flex: 0 0 auto;
}

.mcp-settings__input-group {
    display: flex;
    align-items: stretch;
    gap: 8px;
}

.mcp-settings__input-group .mcp-settings__input {
    flex: 1 1 auto;
    min-width: 0;
}

.mcp-settings__input-group .mcp-settings__button {
    flex: 0 0 auto;
}

.mcp-settings__label {
    font-size: 13px;
    font-weight: 600;
    margin-bottom: 4px;
}

.mcp-settings__input,
.mcp-settings__textarea {
    border-radius: 4px;
    border: 1px solid #ccc;
    padding: 6px 8px;
    font-size: 13px;
    font-family: inherit;
    box-sizing: border-box;
}

.mcp-settings__textarea {
    width: 100%;
    resize: vertical;
}

.mcp-settings__helper-text {
    font-size: 12px;
}

.mcp-settings__helper-text--danger {
    color: #d32f2f;
}

.mcp-settings__helper-text--success {
    color: #2e7d32;
}

.mcp-settings__checkbox-row {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 8px;
}

.mcp-settings__tool-category {
    margin-bottom: 16px;
}

.mcp-settings__tool-category:last-child {
    margin-bottom: 0;
}

.mcp-settings__tool-category-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    margin-bottom: 8px;
    flex-wrap: wrap;
}

.mcp-settings__tool-category-title {
    margin: 0;
    font-size: 13px;
    font-weight: 600;
    color: #444;
}

.mcp-settings__tool-category-actions {
    display: flex;
    gap: 4px;
}

.mcp-settings__category-btn {
    padding: 2px 8px;
    font-size: 11px;
    border-radius: 4px;
    border: 1px solid #ccc;
    background: #f5f5f5;
    cursor: pointer;
    color: #555;
}

.mcp-settings__category-btn:hover {
    background: #e8e8e8;
    color: #333;
}

.mcp-settings__checkbox-label {
    font-size: 13px;
}

.mcp-settings__rule {
    border: 1px solid #e0e0e0;
    border-radius: 6px;
    padding: 10px 12px;
    margin-bottom: 12px;
    background: #fff;
}

.mcp-settings__rule-header {
    display: flex;
    gap: 8px;
    margin-bottom: 8px;
}

.mcp-settings__code-block {
    background: #1e1e1e;
    color: #f5f5f5;
    padding: 12px;
    border-radius: 4px;
    font-size: 12px;
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
}

.mcp-settings__code-block-wrapper {
    position: relative;
}

.mcp-settings__footer {
    position: fixed;

    width: 918px;
    bottom: 0;
    left: 0;
    right: 0;
    border-top: 1px solid #e0e0e0;
    background: #ffffff;
    padding: 8px 12px;
    display: flex;
    align-items: center;
}

.mcp-settings__footer-right {
    display: flex;
    gap: 8px;
    align-items: center;
    margin-left: auto;
}

.mcp-settings__message {
    font-size: 12px;
    color: #8a6d3b;
    background: #fff3cd;
    border-radius: 4px;
}

.mcp-settings__button {
    border-radius: 4px;
    border: 1px solid #ccc;
    padding: 6px 12px;
    font-size: 13px;
    cursor: pointer;
    background: #fff;
}

.mcp-settings__button--primary {
    background: #1976d2;
    border-color: #1976d2;
    color: #fff;
}

.mcp-settings__goose-add-link {
    width: 200px;
    text-decoration: none;
}

.mcp-settings__button--success {
    background: #2e7d32;
    border-color: #2e7d32;
    color: #fff;
}

.mcp-settings__button--secondary {
    background: #f5f5f5;
}

.mcp-settings__button--danger {
    background: #d32f2f;
    border-color: #d32f2f;
    color: #fff;
}

.mcp-settings__button--copy {
    position: absolute;
    top: 8px;
    right: 8px;
    padding: 4px 8px;
    font-size: 11px;
    background: transparent;
    border-color: #ffffff;
    color: #ffffff;
}

.mcp-settings__button:disabled {
    opacity: 0.6;
    cursor: default;
}

.mcp-settings__tabs {
    display: flex;
    gap: 8px;
    padding: 0;
    margin: 0 0 12px;
    list-style: none;
    border-bottom: 1px solid #e0e0e0;
}

.mcp-settings__tab-item {
    margin: 0;
}

.mcp-settings__tab-link {
    border: none;
    background: transparent;
    padding: 6px 12px;
    font-size: 13px;
    cursor: pointer;
    border-bottom: 2px solid transparent;
}

.mcp-settings__tab-link--active {
    border-bottom-color: #1976d2;
    font-weight: 600;
}

.mcp-settings__tab-content {
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    padding: 12px;
    background: #ffffff;
}

.mcp-settings__tab-pane {
    display: block; /* v-show handles visibility */
}

.mcp-settings__text {
    font-size: 12px;
    color: #666666;
}

.mcp-settings__spinner {
    width: 18px;
    height: 18px;
    border: 2px solid #e0e0e0;
    border-top-color: #1976d2;
    border-radius: 50%;
    animation: mcp-settings-spin 0.8s linear infinite;
}

.mcp-settings__spinner-text {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}

@keyframes mcp-settings-spin {
    to {
        transform: rotate(360deg);
    }
}
</style> 