<template>
  <div>
    <ChatView
      v-if="!showSettings && !showMcpSettings"
      @open-settings="showSettings = true"
      @open-mcp-settings="showMcpSettings = true"
    />
    <AISettings v-if="showSettings" @close="showSettings = false" />
    <MCPSettings v-if="showMcpSettings" @close="showMcpSettings = false" />
  </div>
</template>

<script>
import ChatView from "./components/ChatView.vue";
import AISettings from "./components/AISettings.vue";
import MCPSettings from "./components/MCPSettings.vue";

export default {
  components: { ChatView, AISettings, MCPSettings },
  data() {
    return {
      showSettings: false,
      showMcpSettings: false
    };
  }
};
</script>

<style>
html,
body {
  margin: 0;
  padding: 0;
}
</style>

