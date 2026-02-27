<template>
  <div class="ai-settings">
    <header class="ai-settings__header">
      <h2 class="ai-settings__title">AI Settings</h2>
    </header>

    <main class="ai-settings__main">
      <section class="ai-settings__section">
        <h3 class="ai-settings__section-title">API</h3>
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
            Get Anthropic API key
          </a>
        </div>
        <div class="ai-settings__field ai-settings__field--inline">
          <label for="maxTokens" class="ai-settings__label">Max tokens to generate</label>
          <input
            id="maxTokens"
            type="number"
            class="ai-settings__input ai-settings__input--short"
            v-model="maxTokens"
          />
        </div>
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
      </section>

      <section class="ai-settings__section">
        <h3 class="ai-settings__section-title">Tools</h3>
        <div class="ai-settings__field">
          <label class="ai-settings__label">Activated tools</label>
          <div
            v-for="tool in tools"
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
           maxTokens: 0
        };
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
                this.autoReadonlyTools = data.autoReadonlyTools || false;
                this.autoWriteTools = data.autoWriteTools || false;
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
                  autoReadonlyTools: this.autoReadonlyTools,
                  autoWriteTools: this.autoWriteTools
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
    position: sticky;
    bottom: 0;
    margin-top: 24px;    
    padding-top: 12px;
    padding-bottom: 12px;
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

