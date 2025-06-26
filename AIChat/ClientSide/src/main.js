import Vue from 'vue'
import AIChat from './components/AIChat2.vue';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap-icons/font/bootstrap-icons.css';

import "prismjs/themes/prism.css"; // you can change

Vue.config.productionTip = false

new Vue({
  render: h => h(AIChat),
}).$mount('#aichat-container')
