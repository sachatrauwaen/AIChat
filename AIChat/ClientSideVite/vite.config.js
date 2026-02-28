import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import cssInjectedByJs from "vite-plugin-css-injected-by-js";

export default defineConfig({
  plugins: [vue(), cssInjectedByJs()],
  build: {
    outDir: "../scripts/bundles",
    //outDir: "c:/sacha/dnn/DNN910/DesktopModules/Admin/Dnn.PersonaBar/Modules/Satrabel.AIChat/scripts/bundles",
    emptyOutDir: true,
    cssCodeSplit: false,
    rollupOptions: {
      output: {
        entryFileNames: "js/app.js",
        chunkFileNames: "js/[name].js",
        assetFileNames: "assets/[name][extname]"
      }
    }
  }
});
