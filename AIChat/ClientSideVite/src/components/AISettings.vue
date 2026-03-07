<template>
  <div class="ai-settings">
    <header class="ai-settings__header">
      <h2 class="ai-settings__title">AI Settings</h2>
    </header>

    <main class="ai-settings__main">
      <section class="ai-settings__section">
        <h3 class="ai-settings__section-title">API</h3>
        <div class="ai-settings__field">
          <label for="model" class="ai-settings__label">Model</label>
          <select
            id="model"
            class="ai-settings__input"
            v-model="model"
          >
            <option v-for="model in models" :key="model.value" :value="model.value">
              {{ model.name }}
            </option>
          </select>
        </div>
        <div class="ai-settings__field">
          <label for="apiKey" class="ai-settings__label">API Key</label>
          <input
            id="apiKey"
            type="password"
            class="ai-settings__input"
            v-model="apiKey"
            placeholder="Enter your API key"
          />
          <a
            href="https://console.anthropic.com"
            target="_blank"
            class="ai-settings__link"
          >
            Get Anthropic API key for example
          </a>
        </div>
        <div class="ai-settings__field ai-settings__field--inline">
          <label for="maxTokens" class="ai-settings__label">Max tokens to generate</label>
          <input
            id="maxTokens"
            type="number"
            min="1024"
            step="1024"
            class="ai-settings__input ai-settings__input--short"
            v-model="maxTokens"
          />
        </div>
        <div class="ai-settings__field ai-settings__field--inline">
          <label for="historyMaxTokens" class="ai-settings__label">
            Max tokens for conversation history context
          </label>
          <input
            id="historyMaxTokens"
            type="number"
            min="1024"
            step="1024"
            class="ai-settings__input ai-settings__input--short"
            v-model="historyMaxTokens"
          />
        </div>
        <div class="ai-settings__field ai-settings__field--inline">
          <label for="historyMaxTurns" class="ai-settings__label">
            Max recent user turns to keep in context
          </label>
          <input
            id="historyMaxTurns"
            type="number"
            min="1"
            step="1"
            class="ai-settings__input ai-settings__input--short"
            v-model="historyMaxTurns"
          />
        </div>
        
      </section>

      <section class="ai-settings__section">
        <h3 class="ai-settings__section-title">Tools</h3>
        <p class="ai-settings__section-description">
          Tools are used to extend the capabilities of the AI. Activate only the tools you need.
        </p>
        <div class="ai-settings__field">
          <label class="ai-settings__label">Activated tools</label>
          <template v-for="(categoryTools, category) in toolsByCategory" :key="category">
            <div class="ai-settings__tool-category" v-if="categoryTools.length">
              <div class="ai-settings__tool-category-header">
                <h4 class="ai-settings__tool-category-title">{{ category || 'Other' }}</h4>
                <div class="ai-settings__tool-category-actions">
                  <button
                    type="button"
                    class="ai-settings__category-btn"
                    @click="selectAllInCategory(categoryTools)"
                  >
                    Select all
                  </button>
                  <button
                    type="button"
                    class="ai-settings__category-btn"
                    @click="deselectAllInCategory(categoryTools)"
                  >
                    Deselect all
                  </button>
                </div>
              </div>
              <div
                v-for="tool in categoryTools"
                :key="tool.name"
                class="ai-settings__checkbox-row"
              >
                <input
                  type="checkbox"
                  class="ai-settings__checkbox"
                  v-model="tool.active"
                  :id="tool.name"
                />
                <label :for="tool.name" class="ai-settings__checkbox-label">
                  <span class="ai-settings__checkbox-name">{{ tool.name }}</span>
                  <span class="ai-settings__checkbox-description">{{ tool.description }}</span>
                </label>
              </div>
            </div>
          </template>
        </div>

        <div class="ai-settings__field">
          <label class="ai-settings__label">Automatic execution of tools</label>
          <div class="ai-settings__checkbox-row">
            <input
              type="checkbox"
              class="ai-settings__checkbox"
              v-model="autoReadonlyTools"
              id="autoReadonlyTools"
            />
            <label for="autoReadonlyTools" class="ai-settings__checkbox-label">
              Readonly tools
            </label>
          </div>
          <div class="ai-settings__checkbox-row">
            <input
              type="checkbox"
              class="ai-settings__checkbox"
              v-model="autoWriteTools"
              id="autoWriteTools"
            />
            <label for="autoWriteTools" class="ai-settings__checkbox-label">
              Write tools
            </label>
          </div>
        </div>

        <div class="ai-settings__field">
          <label class="ai-settings__label">Debugging</label>
          <div class="ai-settings__checkbox-row">
            <input
              type="checkbox"
              class="ai-settings__checkbox"
              v-model="debug"
              id="debug"
            />
            <label for="debug" class="ai-settings__checkbox-label">
              Debug mode (Browser console will show detailed messages)
            </label>
          </div>
        </div>
      </section>

      <section class="ai-settings__section">
        <h3 class="ai-settings__section-title">Instructions</h3>
        <div class="ai-settings__field">
          <label for="globalRules" class="ai-settings__label">Global instructions</label>
          <textarea
            id="globalRules"
            class="ai-settings__textarea"
            v-model="globalRules"
            rows="5"
            placeholder="Enter your instructions"
          ></textarea>
        </div>

        <div class="ai-settings__field">
          <label class="ai-settings__label">Specific rules</label>
          <div
            v-for="(rule, index) in rules"
            :key="index"
            class="ai-settings__rule"
          >
            <div class="ai-settings__rule-header">
              <input
                type="text"
                class="ai-settings__input"
                v-model="rule.name"
                placeholder="Rule name"
                pattern="^[^\\/:*?&quot;<>|]+$"
                title="Please enter a valid file name without special characters"
              />
              <button
                type="button"
                class="ai-settings__button ai-settings__button--danger"
                @click="deleteRule(index)"
              >
                Delete
              </button>
            </div>
            <textarea
              class="ai-settings__textarea"
              v-model="rule.rule"
              rows="5"
              placeholder="Enter your instructions"
            ></textarea>
          </div>
          <button
            type="button"
            class="ai-settings__button ai-settings__button--secondary ai-settings__add-rule"
            @click="addRule"
          >
            Add new rule
          </button>
        </div>
      </section>
    </main>

    <footer class="ai-settings__footer">
      <div class="ai-settings__footer-left">
        <span v-if="message" class="ai-settings__message">{{ message }}</span>
      </div>
      <div class="ai-settings__footer-right">
        <button
          type="button"
          class="ai-settings__button ai-settings__button--secondary"
          @click="cancel"
        >
          Cancel
        </button>
        <button
          type="button"
          class="ai-settings__button ai-settings__button--primary"
          @click="save"
          :disabled="isThinking"
        >
          <span v-if="isThinking">Saving...</span>
          <span v-else>Save</span>
        </button>
      </div>
    </footer>
  </div>
</template>

<script>
import { getSettings, saveSettings } from "../api/aiTornadoService";


export default {
    mounted() {
        // Load initial data
        this.loadSettings();
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
           autoWriteTools: false,
           maxTokens: 0,
           historyMaxTokens: 0,
           historyMaxTurns: 0,
           debug: false
        };
    },
    computed: {
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
        async loadSettings() {
            this.isThinking = true;
            try {
                const data = await getSettings();
                this.apiKey = data.apiKey || '';
                this.model = data.model || '';
                this.models = data.models || [];
                this.rules = data.rules || [];
                this.globalRules= data.globalRules || '';
                this.tools = data.tools || [];
                this.maxTokens = data.maxTokens || 1024;
                this.historyMaxTokens = data.historyMaxTokens ?? 8192;
                this.historyMaxTurns = data.historyMaxTurns || 20;
                this.autoReadonlyTools = data.autoReadonlyTools || false;
                this.autoWriteTools = data.autoWriteTools || false;
                this.debug = data.debug || false;
                this.isThinking = false;
            } catch (e) {
                this.message = e.message || "Failed to load settings.";
                this.isThinking = false;
            }
        },
        async save() {
            this.isThinking = true;
            try {
              await saveSettings({
                  apiKey: this.apiKey,
                  model: this.model,
                  rules: this.rules,
                  globalRules: this.globalRules,
                  tools: this.tools,
                  maxTokens: this.maxTokens,
                  historyMaxTokens: this.historyMaxTokens,
                  historyMaxTurns: this.historyMaxTurns,
                  autoReadonlyTools: this.autoReadonlyTools,
                  autoWriteTools: this.autoWriteTools,
                  debug: this.debug
              });
            
                this.isThinking = false;
                this.message = 'Settings saved successfully';                    
                this.$emit('close');
            } catch (e) {
              this.message = e.message || "Failed to save settings.";
              this.isThinking = false;
            }
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
        errorCallback(error) {
            this.isThinking = false;
            this.message = error && error.message ? error.message : 'An error occurred.';
        }
    }
};
</script>

<style scoped>

.ai-settings {
    max-width: 900px;
    margin: 0 auto;
    padding: 24px 24px 80px;
    box-sizing: border-box;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
}

.ai-settings__header {
    border-bottom: 1px solid #e0e0e0;
    padding-bottom: 12px;
    margin-bottom: 16px;
}

.ai-settings__title {
    margin: 0;
    font-size: 22px;
    font-weight: 600;
}

.ai-settings__main {
    display: flex;
    flex-direction: column;
    gap: 24px;
}

.ai-settings__section {
    padding: 16px 20px;
    border-radius: 8px;
    background: #fafafa;
    border: 1px solid #e0e0e0;
}

.ai-settings__section-title {
    margin: 0 0 12px;
    font-size: 16px;
    font-weight: 600;
}

.ai-settings__section-description {
    position: relative;
    margin: 0 0 14px;
    padding: 8px 10px 8px 32px;
    font-size: 12px;
    line-height: 1.5;
    color: #555;
    background: #fffdf5;
    border-radius: 4px;
    border: 1px solid #ffe0a3;
}

.ai-settings__section-description::before {
    content: "Tip";
    position: absolute;
    left: 10px;
    top: 50%;
    transform: translateY(-50%);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: #b26a00;
}

.ai-settings__field {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-bottom: 16px;
}

.ai-settings__field--inline {
    max-width: 320px;
}

.ai-settings__label {
    font-size: 13px;
    font-weight: 500;
}

.ai-settings__input,
.ai-settings__textarea,
.ai-settings__select {
    border-radius: 4px;
    border: 1px solid #ccc;
    padding: 6px 8px;
    font-size: 13px;
    font-family: inherit;
    box-sizing: border-box;
}

.ai-settings__input--short {
    max-width: 160px;
}

.ai-settings__textarea {
    width: 100%;
    min-height: 150px;
    resize: vertical;
}

.ai-settings__link {
    font-size: 12px;
    color: #1976d2;
    text-decoration: none;
}

.ai-settings__link:hover {
    text-decoration: underline;
}

.ai-settings__checkbox-row {
    display: flex;
    align-items: flex-start;
    gap: 8px;
    margin-bottom: 8px;
}

.ai-settings__tool-category {
    margin-bottom: 16px;
}

.ai-settings__tool-category:last-child {
    margin-bottom: 0;
}

.ai-settings__tool-category-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    margin-bottom: 8px;
    flex-wrap: wrap;
}

.ai-settings__tool-category-title {
    margin: 0;
    font-size: 13px;
    font-weight: 600;
    color: #444;
}

.ai-settings__tool-category-actions {
    display: flex;
    gap: 4px;
}

.ai-settings__category-btn {
    padding: 2px 8px;
    font-size: 11px;
    border-radius: 4px;
    border: 1px solid #ccc;
    background: #f5f5f5;
    cursor: pointer;
    color: #555;
}

.ai-settings__category-btn:hover {
    background: #e8e8e8;
    color: #333;
}

.ai-settings__checkbox {
    margin-top: 3px;
}

.ai-settings__checkbox-label {
    font-size: 13px;
}

.ai-settings__checkbox-name {
    font-weight: 500;
    margin-right: 4px;
}

.ai-settings__checkbox-description {
    color: #666;
}

.ai-settings__rule {
    border: 1px solid #e0e0e0;
    border-radius: 6px;
    padding: 10px 12px;
    margin-bottom: 12px;
    background: #fff;
}

.ai-settings__rule-header {
    display: flex;
    gap: 8px;
    margin-bottom: 8px;
}

.ai-settings__add-rule {
    margin-top: 4px;
}

.ai-settings__footer {
    position: fixed;
    bottom: 0;
    width: 918px;
    left: 0;    
    margin-top: 24px;    
    padding: 12px;
    border-top: 1px solid #e0e0e0;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    background: #fff;
}

.ai-settings__footer-right {
    display: flex;
    gap: 8px;
}

.ai-settings__message {
    font-size: 12px;
    color: #8a6d3b;
    background: #fff3cd;
    border-radius: 4px;
    padding: 4px 8px;
}

.ai-settings__button {
    border-radius: 4px;
    border: 1px solid #ccc;
    padding: 6px 12px;
    font-size: 13px;
    cursor: pointer;
    background: #fff;
}

.ai-settings__button--primary {
    background: #1976d2;
    border-color: #1976d2;
    color: #fff;
}

.ai-settings__button--secondary {
    background: #f5f5f5;
}

.ai-settings__button--danger {
    background: #d32f2f;
    border-color: #d32f2f;
    color: #fff;
}

.ai-settings__button:disabled {
    opacity: 0.6;
    cursor: default;
}

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

