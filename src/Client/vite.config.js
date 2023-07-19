//
import { defineConfig  } from 'vite'

export default defineConfig({
    // vite has its own webserver
    // see Layout.fs where it references "@vite" package
    base: '/dist/',
    define: {
      // window not available, so make it available
      global: "window",
    },
    build: {
        outDir: '../Server/WebRoot/dist',
        emptyOutDir: true,
        manifest: false,
        rollupOptions: {

          input: {
            main: './build/App.js',
          },
          manualChunks: (id) =>{
            if (id.includes('fable')) {
              return 'fable';
            }
            if (id.includes('lit')) {
              return 'lit';
            }
          }
        }
      },
    server: {
        watch: {
          ignored: [ "**/*.fs"]
        },

        https: false,
        strictPort: true,
        hmr: {
          clientPort: 5173,
          protocol: 'ws'
        }
      }
})
