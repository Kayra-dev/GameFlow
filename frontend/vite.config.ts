import { fileURLToPath, URL } from 'node:url';

import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  // GitHub Pages alt dizinde yayınlandığı için taban yol ortam değişkeninden alınır.
  base: process.env.VITE_BASE_PATH ?? '/',
  server: {
    port: 5173,
    strictPort: true,
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
    rollupOptions: {
      output: {
        // Büyük kütüphaneleri ayrı parçalara bölerek ilk yüklemeyi hızlandırır.
        manualChunks(id) {
          if (!id.includes('node_modules')) return undefined;
          if (/[\\/]node_modules[\\/](react|react-dom|react-router)/.test(id)) return 'react';
          if (id.includes('@microsoft/signalr')) return 'realtime';
          if (id.includes('recharts') || id.includes('d3-')) return 'charts';
          if (id.includes('framer-motion') || id.includes('motion-dom')) return 'motion';
          if (/(@tanstack|axios|zustand)/.test(id)) return 'data';
          return 'vendor';
        },
      },
    },
  },
});
