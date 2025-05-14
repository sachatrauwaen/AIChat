import Vue from 'vue'
import AIChat from './components/AIChat.vue';
import 'bootstrap/dist/css/bootstrap.min.css';

import "prismjs/themes/prism.css"; // you can change

Vue.config.productionTip = false

new Vue({
  render: h => h(AIChat),
}).$mount('#aichat-container')
