import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
    dedupe: ['react', 'react-dom', 'react-router-dom', 'zustand'],
  },
  optimizeDeps: {
    include: ['react', 'react-dom', 'react-router-dom', 'zustand', '@tanstack/react-query'],
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5005',
        changeOrigin: true,
      },
      '/captures': {
        target: 'http://localhost:5005',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5005',
        ws: true,
        changeOrigin: true,
      },
    },
  },
})
