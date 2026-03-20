<template>
  <div class="chat-layout">
    <aside class="sidebar">
      <div class="sidebar-top">
        <h3>Conversations</h3>
        <button class="btn" @click="newConversation">New Chat</button>
      </div>
      <ul class="conversation-list">
        <li
          v-for="c in conversations"
          :key="c.id"
          :class="{ active: c.id === conversationId }"
        >
          <div class="conv-row" @click="loadConv(c.id)">
            <div class="title">{{ c.title }}</div>
            <div class="meta">{{ formatDate(c.lastModified || c.createdAt) }}</div>
          </div>
          <button class="delete-btn" title="Delete" @click.stop="deleteConv(c.id)">&times;</button>
        </li>
      </ul>
      <div class="sidebar-bottom" style="display: flex; gap: 8px;">
        <button class="btn settings-btn" @click="openSettings" style="flex: 1;">
          <span class="settings-icon">⚙</span>
          <span>Settings</span>
        </button>
        <button class="btn settings-btn" @click="openMcpSettings" style="flex: 1;">
          <span class="settings-icon">🔌</span>
          <span>MCP</span>
        </button>
      </div>
    </aside>
    <main class="chat-main">
      <div class="messages" ref="messagesEl">
        <div v-if="!filteredMessages.length && !streamingText" class="new-chat-intro">
          <h1 class="chat-intro-title">How can I help you today?</h1>
          <h3 class="chat-intro-subtitle">I am your DNN AI assistant.</h3>
          <div class="new-chat-buttons">
            <button
              class="chat-suggestion-btn"
              @click="userInput='List pages of this website'"
            >
              List pages of this website
            </button>
            <button
              class="chat-suggestion-btn"
              @click="userInput='Send a email with a joke to info@example.com'"
            >
              Send a email
            </button>
            <button
              class="chat-suggestion-btn"
              @click="userInput='Generate a seo report of the home page in a table'"
            >
              Make a seo report of the home page
            </button>
          </div>
        </div>
        <div v-else>
          <div v-for="(m, idx) in filteredMessages" :key="idx" class="message" :class="m.role">
            <template v-if="m.role === 'tool'">
              <details class="tool-history">
                <summary class="tool-summary">
                  <span class="tool-badge">Tool</span>
                  <strong>{{ m.toolName }}</strong>
                </summary>
                <div class="tool-history-body">
                  <div v-if="m.toolArguments" class="tool-history-section">
                    <div class="tool-history-label">Arguments</div>
                    <pre class="tool-history-pre">{{ JSON.stringify(m.toolArguments, null, 2) }}</pre>
                  </div>
                  <div v-if="m.content" class="tool-history-section">
                    <div class="tool-history-label">Result</div>
                    <pre class="tool-history-pre">{{ m.content }}</pre>
                  </div>
                </div>
              </details>
            </template>
            <template v-else>
              <div class="role-label">{{ m.role }}</div>
              <div class="content" v-html="renderMarkdown(m.content)"></div>
            </template>
          </div>
          <div v-if="isThinking && !streamingText" class="message assistant">
            <div class="role-label">assistant</div>
            <div v-if="waitMessage" class="content wait-indicator">{{ waitMessage }}</div>
            <div v-else class="content thinking-indicator">
              <template v-if="executingTools.length">
                <span class="executing-tools-label">Running</span>
                <span v-for="(t, i) in executingTools" :key="t" class="executing-tool-name">{{ t }}<span v-if="i < executingTools.length - 1">,&nbsp;</span></span>
              </template>
              <template v-else>
                <span class="thinking-letter" style="animation-delay: 0s">D</span>
                <span class="thinking-letter" style="animation-delay: 0.15s">N</span>
                <span class="thinking-letter" style="animation-delay: 0.3s">N</span>
              </template>
              <span class="thinking-dot" style="animation-delay: 0.5s">.</span>
              <span class="thinking-dot" style="animation-delay: 0.65s">.</span>
              <span class="thinking-dot" style="animation-delay: 0.8s">.</span>
            </div>
          </div>
          <div v-if="streamingText" class="message assistant">
            <div class="role-label">assistant</div>
            <div class="content" v-html="renderMarkdown(streamingText)"></div>
          </div>
        </div>
      </div>

      <div v-if="toolCall" class="tool-confirm">
        <div class="tool-header">
          <span class="tool-badge">Tool</span>
          <strong>{{ toolCall.name }}</strong>
          <span v-if="pendingToolCalls.length > 0" class="tool-queue-info">
            (+{{ pendingToolCalls.length }} more)
          </span>
        </div>
        <pre v-if="toolCall.description" class="tool-args">{{ toolCall.description }}</pre>
        <div class="tool-actions">
          <button class="btn primary" :disabled="isThinking" @click="runTool">
            {{ isThinking ? "Running..." : "Run" }}
          </button>
          <button class="btn" :disabled="isThinking" @click="cancelTool">Cancel</button>
        </div>
      </div>

      <div class="input-panel">
        <div class="input-row">
          <textarea
            v-model="userInput"
            class="input"
            rows="3"
            placeholder="Ask a question..."
            @keydown.enter.exact.prevent="send"
          />
          
        </div>
        <div class="input-footer">
          <span v-if="totalPrice" class="price-info">
            ${{ totalPrice.toFixed(5) }} ({{ totalInputTokens }} in / {{ totalOutputTokens }} out)
          </span>
         
          <div v-if="rules.length > 0" class="rules-dropdown-wrap" ref="rulesDropdownWrap">
            <button type="button" class="btn rules-plus-btn" title="Rules" @click="showRulesDropdown = !showRulesDropdown">+</button>
            <div v-show="showRulesDropdown" class="rules-dropdown" ref="rulesDropdown">
              <div class="rules-dropdown-title">Include</div>
              <label class="rules-dropdown-item">
                <input type="checkbox" v-model="includeCurrentPage" />
                <span>Current Page</span>
              </label>
              <div class="rules-dropdown-sep"></div>
              <div class="rules-dropdown-title">Skills</div>
              <label v-for="r in rules" :key="r" class="rules-dropdown-item">
                <input type="checkbox" :checked="selectedRules.includes(r)" @change="toggleRule(r)" />
                <span>{{ r }}</span>
              </label>
            </div>
          </div>
          <select v-model="selectedMode" class="mode-select">
            <option value="chat">Chat</option>
            <option value="readonly">Agent (Read Only)</option>
            <option value="agent">Agent (Read/Write)</option>
          </select>
          <button v-if="isThinking" class="btn stop-btn" @click="stopStreaming">
            Stop
          </button>
          <button v-else class="btn primary" :disabled="!userInput || !!toolCall" @click="send">
            Send
          </button>
        </div>
        <div v-if="error" class="error">{{ error }}</div>
      </div>
    </main>
  </div>
</template>

<script>
import { marked } from "marked";
import DOMPurify from "dompurify";
import { tornadoChat, tornadoChatStream, getInfo, getConversations, loadConversation, deleteConversation, saveChatPreferences } from "../api/aiTornadoService";

export default {
  computed: {
    filteredMessages() {
      return this.messages.filter(m =>
        !(m.role === 'user' && m.content && m.content.startsWith('[Tool Result for '))
      );
    },
    effectiveRules() {
      const parts = (this.includeCurrentPage ? ['Current Page'] : []).concat(this.selectedRules);
      return parts.join(', ');
    }
  },
  data() {
    return {
      conversations: [],
      conversationId: null,
      messages: [],
      userInput: "",
      isThinking: false,
      error: "",
      toolCall: null,
      pendingToolCalls: [],
      selectedMode: "readonly",
      rules: [],
      selectedRules: [],
      showRulesDropdown: false,
      includeCurrentPage: false,
      totalPrice: 0,
      totalInputTokens: 0,
      totalOutputTokens: 0,
      _preferencesLoaded: false,
      debug: false,
      streamingText: "",
      waitMessage: "",
      executingTools: [],
      _abortController: null,
      _waitTimer: null
    };
  },
  watch: {
    selectedMode(val) {
      if (this._preferencesLoaded) {
        // saveChatPreferences({ selectedMode: val }).catch(() => {});
      }
    }
  },
  mounted() {
    this.fetchConversations();
    this.loadInfo();
    document.addEventListener('click', this.handleRulesClickOutside);
  },
  beforeDestroy() {
    document.removeEventListener('click', this.handleRulesClickOutside);
  },
  methods: {
    scrollToBottom() {
      this.$nextTick(() => {
        const el = this.$refs.messagesEl;
        if (el) el.scrollTop = el.scrollHeight;
      });
    },
    async fetchConversations() {
      try {
        this.conversations = await getConversations();
      } catch (e) {
        this.error = e.message || "Failed to load conversations.";
      }
    },
    async loadInfo() {
      try {
        const data = await getInfo();
        this.rules = (data && data.rules) ? data.rules : [];
        this.selectedMode = (data && data.selectedMode != null) ? data.selectedMode : "readonly";
        this.selectedRules = (data && data.selectedRules && data.selectedRules.length) ? data.selectedRules : [];
        this.includeCurrentPage = !!(data && data.includeCurrentPage);
        this.debug = !!(data && data.debug);
        this._preferencesLoaded = true;
      } catch (e) {
        this.rules = [];
        this._preferencesLoaded = true;
      }
    },
    async loadConv(id) {
      try {
        const res = await loadConversation(id);
        if (res.success) {
          this.conversationId = res.conversationId;
          this.messages = res.messages || [];
          this.toolCall = null;
          this.pendingToolCalls = [];
          this.error = "";
          this.scrollToBottom();
        } else {
          this.error = res.message || "Failed to load conversation.";
        }
      } catch (e) {
        this.error = e.message || "Failed to load conversation.";
      }
    },
    async deleteConv(id) {
      try {
        await deleteConversation(id);
        this.conversations = this.conversations.filter(c => c.id !== id);
        if (this.conversationId === id) {
          this.newConversation();
        }
      } catch (e) {
        this.error = e.message || "Failed to delete.";
      }
    },
    newConversation() {
      this.stopStreaming();
      this.conversationId = null;
      this.messages = [];
      this.userInput = "";
      this.error = "";
      this.toolCall = null;
      this.pendingToolCalls = [];
      this.totalPrice = 0;
      this.totalInputTokens = 0;
      this.totalOutputTokens = 0;
      this.streamingText = "";
    },
    stopStreaming() {
      if (this._abortController) {
        this._abortController.abort();
        this._abortController = null;
      }
      this.clearWaitTimer();
      this.executingTools = [];
      this.commitStreamingText();
      this.isThinking = false;
    },
    commitStreamingText() {
      if (this.streamingText) {
        this.messages.push({ role: "assistant", content: this.streamingText });
        this.streamingText = "";
      }
    },
    clearWaitTimer() {
      if (this._waitTimer) {
        clearInterval(this._waitTimer);
        this._waitTimer = null;
      }
      this.waitMessage = "";
    },
    toggleRule(rule) {
      const i = this.selectedRules.indexOf(rule);
      if (i >= 0) this.selectedRules.splice(i, 1);
      else this.selectedRules.push(rule);
    },
    handleRulesClickOutside(e) {
      const wrap = this.$refs.rulesDropdownWrap;
      if (this.showRulesDropdown && wrap && !wrap.contains(e.target)) this.showRulesDropdown = false;
    },
    startWaitCountdown(seconds) {
      this.clearWaitTimer();
      let remaining = seconds;
      this.waitMessage = `Rate limit exceeded. Retrying in ${remaining}s...`;
      this._waitTimer = setInterval(() => {
        remaining--;
        if (remaining <= 0) {
          this.clearWaitTimer();
        } else {
          this.waitMessage = `Rate limit exceeded. Retrying in ${remaining}s...`;
        }
      }, 1000);
    },
    async send() {
      if (!this.userInput || this.toolCall) return;
      this.isThinking = true;
      this.error = "";
      this.streamingText = "";

      const sentMessage = this.userInput;
      this.messages.push({ role: "user", content: sentMessage });
      this.userInput = "";
      this.scrollToBottom();

      this._abortController = new AbortController();
      try {
        await tornadoChatStream({
          conversationId: this.conversationId,
          message: sentMessage,
          runTool: false,
          toolCallId: null,
          toolName: null,
          toolArguments: null,
          mode: this.selectedMode,
          selectedRules: this.selectedRules,
          includeCurrentPage: this.includeCurrentPage
        }, {
          signal: this._abortController.signal,
          onToken: (text) => {
            this.clearWaitTimer();
            this.streamingText += text;
            this.scrollToBottom();
          },
          onToolStart: (data) => {
            this.commitStreamingText();
            if (!this.executingTools.includes(data.toolName)) {
              this.executingTools.push(data.toolName);
            }
            this.scrollToBottom();
          },
          onAutoTool: (data) => {
            this.clearWaitTimer();
            this.executingTools = this.executingTools.filter(t => t !== data.toolName);
            this.messages.push({ role: "tool", toolName: data.toolName, content: data.result });
            this.scrollToBottom();
          },
          onWait: (data) => {
            this.startWaitCountdown(data.seconds || 30);
          },
          onDone: (res) => {
            this.clearWaitTimer();
            this.executingTools = [];
            this.streamingText = "";
            if (res.success) {
              if (this.debug && res.debugMessages) {
                console.log("[AIChat Debug] Conversation messages (send)", res.debugMessages);
              }
              this.conversationId = res.conversationId;
              this.messages = res.messages || [];
              this.toolCall = res.toolCall || null;
              this.pendingToolCalls = res.pendingToolCalls || [];
              this.totalPrice = res.totalPrice || 0;
              this.totalInputTokens = res.totalInputTokens || 0;
              this.totalOutputTokens = res.totalOutputTokens || 0;
              this.scrollToBottom();
            } else {
              this.error = res.message || "Request failed.";
            }
          },
          onError: (msg) => {
            this.clearWaitTimer();
            this.streamingText = "";
            this.error = msg || "Request failed.";
          }
        });
      } catch (e) {
        if (e.name === "AbortError") return;
        this.clearWaitTimer();
        this.streamingText = "";
        this.error = e.message || "Request failed.";
      } finally {
        this._abortController = null;
      }
      this.isThinking = false;
      await this.fetchConversations();
    },
    async runTool() {
      if (!this.toolCall) return;
      await this.respondToTool(true);
    },
    async cancelTool() {
      if (!this.toolCall) return;
      await this.respondToTool(false);
    },
    async respondToTool(approved) {
      this.isThinking = true;
      this.error = "";
      this.streamingText = "";

      this._abortController = new AbortController();
      try {
        await tornadoChatStream({
          conversationId: this.conversationId,
          message: null,
          runTool: true,
          toolApproved: approved,
          toolCallId: this.toolCall.id,
          toolName: this.toolCall.name,
          toolArguments: this.toolCall.arguments,
          pendingToolCalls: this.pendingToolCalls.length > 0 ? this.pendingToolCalls : null,
          mode: this.selectedMode,
          selectedRules: this.selectedRules,
          includeCurrentPage: this.includeCurrentPage
        }, {
          signal: this._abortController.signal,
          onToken: (text) => {
            this.clearWaitTimer();
            this.streamingText += text;
            this.scrollToBottom();
          },
          onToolStart: (data) => {
            this.commitStreamingText();
            if (!this.executingTools.includes(data.toolName)) {
              this.executingTools.push(data.toolName);
            }
            this.scrollToBottom();
          },
          onAutoTool: (data) => {
            this.clearWaitTimer();
            this.executingTools = this.executingTools.filter(t => t !== data.toolName);
            this.messages.push({ role: "tool", toolName: data.toolName, content: data.result });
            this.scrollToBottom();
          },
          onWait: (data) => {
            this.startWaitCountdown(data.seconds || 30);
          },
          onDone: (res) => {
            this.clearWaitTimer();
            this.executingTools = [];
            this.streamingText = "";
            if (res.success) {
              if (this.debug && res.debugMessages) {
                console.log("[AIChat Debug] Conversation messages (tool)", res.debugMessages);
              }
              this.conversationId = res.conversationId;
              this.messages = res.messages || [];
              this.toolCall = res.toolCall || null;
              this.pendingToolCalls = res.pendingToolCalls || [];
              this.totalPrice = res.totalPrice || 0;
              this.totalInputTokens = res.totalInputTokens || 0;
              this.totalOutputTokens = res.totalOutputTokens || 0;
              this.scrollToBottom();
            } else {
              this.error = res.message || "Tool operation failed.";
            }
          },
          onError: (msg) => {
            this.clearWaitTimer();
            this.streamingText = "";
            this.error = msg || "Tool operation failed.";
          }
        });
      } catch (e) {
        if (e.name === "AbortError") return;
        this.clearWaitTimer();
        this.streamingText = "";
        this.error = e.message || "Tool operation failed.";
      } finally {
        this._abortController = null;
      }
      this.isThinking = false;
    },
    openSettings() {
      this.$emit("open-settings");
    },
    openMcpSettings() {
      this.$emit("open-mcp-settings");
    },
    renderMarkdown(text) {
      return DOMPurify.sanitize(marked.parse(text || ""));
    },
    formatDate(value) {
      if (!value) return "";
      return new Date(value).toLocaleString();
    }
  }
};
</script>

<style scoped>
.chat-layout {
  display: flex;
  height: 100vh;
  font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}

.sidebar {
  width: 260px;
  border-right: 1px solid #ddd;
  padding: 16px;
  box-sizing: border-box;
  background: #fafafa;
  display: flex;
  flex-direction: column;
}

.sidebar h3 {
  margin-top: 0;
  margin-bottom: 12px;
}

.sidebar-top {
  margin-bottom: 12px;
}

.conversation-list {
  list-style: none;
  padding: 0;
  margin: 12px 0 0;
  flex: 1;
  overflow-y: auto;
}

.conversation-list li {
  display: flex;
  align-items: center;
  padding: 6px 8px;
  border-radius: 4px;
  cursor: pointer;
}

.conversation-list li:hover {
  background: #f0f0f0;
}

.conversation-list li.active {
  background: #e3f2fd;
}

.conv-row {
  flex: 1;
  min-width: 0;
}

.conversation-list .title {
  font-weight: 600;
  font-size: 13px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.conversation-list .meta {
  font-size: 11px;
  color: #666;
}

.sidebar-bottom {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid #eee;
}

.delete-btn {
  background: none;
  border: none;
  font-size: 16px;
  cursor: pointer;
  color: #999;
  padding: 0 4px;
  line-height: 1;
}

.delete-btn:hover {
  color: #b71c1c;
}

.chat-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.messages {
  flex: 1;
  padding: 16px;
  overflow-y: auto;
}

.new-chat-intro {
  max-width: 720px;
  margin: 0 auto;
  text-align: center;
}

.new-chat-intro h1 {
  margin-top: 40px;
  margin-bottom: 8px;
}

.new-chat-intro h3 {
  margin-top: 0;
  color: #555;
  font-weight: 400;
}

.chat-intro-title {
  font-size: 28px;
  font-weight: 600;
}

.chat-intro-subtitle {
  font-size: 18px;
  font-weight: 400;
}

.new-chat-buttons {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 12px;
  margin-top: 32px;
}

.chat-suggestion-btn {
  border-radius: 999px;
  border: 1px solid #ddd;
  background: #fff;
  padding: 8px 16px;
  font-size: 13px;
  cursor: pointer;
  transition: background 0.15s, box-shadow 0.15s, transform 0.05s;
}

.chat-suggestion-btn:hover {
  background: #f5f5f5;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
}

.chat-suggestion-btn:active {
  transform: translateY(1px);
}

.message {
  margin-bottom: 12px;
}

.message .role-label {
  font-size: 11px;
  text-transform: uppercase;
  color: #888;
  margin-bottom: 2px;
}

.message.user .content {
  background: #e3f2fd;
}

.message.assistant .content {
  background: #f5f5f5;  
}

.content {
  padding: 8px 10px;
  border-radius: 4px;
  line-height: 1.5;
}

.content :deep(ol) {
  list-style-type: decimal;
  padding-left: 20px;
}

.content :deep(ul) {
  list-style-type: disc;
  padding-left: 20px;
}

.content :deep(pre) {
  background: #f6f8fa;
  padding: 12px;
  border-radius: 4px;
  overflow-x: auto;
}

.content :deep(code) {
  font-family: SFMono-Regular, Consolas, "Liberation Mono", Menlo, monospace;
  font-size: 13px;
}

.content :deep(table) {
  border-collapse: collapse;
  width: 100%;
  margin: 8px 0;
}

.content :deep(th),
.content :deep(td) {
  border: 1px solid #dfe2e5;
  padding: 6px 10px;
  text-align: left;
}

.message.tool {
  margin-bottom: 6px;
}

.tool-history {
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  background: #f9f9f9;
  overflow: hidden;
}

.tool-summary {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  cursor: pointer;
  list-style: none;
  user-select: none;
  font-size: 13px;
}

.tool-summary::-webkit-details-marker {
  display: none;
}

.tool-summary::before {
  content: "▶";
  font-size: 10px;
  color: #888;
  transition: transform 0.15s;
  display: inline-block;
}

.tool-history[open] .tool-summary::before {
  transform: rotate(90deg);
}

.tool-history-body {
  border-top: 1px solid #e0e0e0;
  padding: 8px 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.tool-history-section {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.tool-history-label {
  font-size: 10px;
  text-transform: uppercase;
  color: #888;
  font-weight: 600;
}

.tool-history-pre {
  background: #f0f0f0;
  padding: 6px 8px;
  border-radius: 4px;
  font-size: 12px;
  margin: 0;
  overflow-x: auto;
  white-space: pre-wrap;
  word-break: break-all;
  max-height: 200px;
  overflow-y: auto;
}

.tool-confirm {
  border-top: 1px solid #ddd;
  padding: 12px 16px;
  background: #fffde7;
}

.tool-header {
  margin-bottom: 6px;
}

.tool-badge {
  background: #333;
  color: #fff;
  font-size: 11px;
  padding: 2px 6px;
  border-radius: 3px;
  margin-right: 6px;
}

.tool-queue-info {
  font-size: 12px;
  color: #666;
  margin-left: 4px;
}

.tool-args {
  background: #f6f8fa;
  padding: 8px;
  border-radius: 4px;
  font-size: 12px;
  max-height: 120px;
  overflow: auto;
  margin: 6px 0;
}

.tool-actions {
  display: flex;
  gap: 8px;
}

.input-panel {
  border-top: 1px solid #ddd;
  padding: 12px 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.input-row {
  display: flex;
  gap: 8px;
}

.input {
  flex: 1;
  padding: 8px;
  resize: vertical;
  box-sizing: border-box;
  border: 1px solid #ccc;
  border-radius: 4px;
}

.mode-select {
  width: 160px;
  padding: 6px;
  border: 1px solid #ccc;
  border-radius: 4px;
  align-self: flex-start;
}

.rules-dropdown-wrap {
  position: relative;
}

.rules-plus-btn {
  min-width: 32px;
  padding: 6px 10px;
  font-size: 16px;
  line-height: 1;
}

.rules-dropdown {
  position: absolute;
  bottom: 100%;
  left: 0;
  margin-bottom: 4px;
  min-width: 180px;
  max-height: 220px;
  overflow-y: auto;
  background: #fff;
  border: 1px solid #ccc;
  border-radius: 4px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  padding: 6px 0;
  z-index: 100;
}

.rules-dropdown-title {
  padding: 2px 12px 4px;
  font-size: 9px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #666;
}

.rules-dropdown-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  cursor: pointer;
  font-size: 13px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.rules-dropdown-item:hover {
  background: #f0f0f0;
}

.rules-dropdown-item input {
  flex-shrink: 0;
}

.rules-dropdown-sep {
  height: 1px;
  margin: 4px 12px;
  background: #ddd;
}

.input-footer {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 12px;
}

.price-info {
  font-size: 11px;
  color: #666;
}

.settings-btn {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.settings-icon {
  font-size: 14px;
}

.btn {
  border: 1px solid #ccc;
  background: #fff;
  padding: 6px 12px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 13px;
}

.btn.primary {
  background: #1976d2;
  border-color: #1976d2;
  color: #fff;
}

.btn.stop-btn {
  background: #c62828;
  border-color: #c62828;
  color: #fff;
}

.btn:disabled {
  opacity: 0.6;
  cursor: default;
}

.error {
  color: #b71c1c;
  font-size: 12px;
}

.wait-indicator {
  color: #e65100;
  font-size: 13px;
  font-weight: 500;
  animation: waitPulse 2s ease-in-out infinite;
}

@keyframes waitPulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.executing-tools-label {
  font-size: 13px;
  color: #888;
  margin-right: 4px;
}

.executing-tool-name {
  font-size: 14px;
  color: #1976d2;
}

.thinking-indicator {
  display: flex;
  align-items: baseline;
  gap: 1px;
  font-weight: 700;
  font-size: 18px;
  color: #1976d2;
  user-select: none;
}

.thinking-letter {
  display: inline-block;
  animation: thinkingBounce 1.2s ease-in-out infinite;
  opacity: 0.3;
}

.thinking-dot {
  display: inline-block;
  animation: thinkingBounce 1.2s ease-in-out infinite;
  opacity: 0.3;
  font-size: 22px;
  line-height: 0.5;
}

@keyframes thinkingBounce {
  0%, 100% {
    opacity: 0.25;
    transform: translateY(0);
  }
  50% {
    opacity: 1;
    transform: translateY(-4px);
  }
}
</style>
