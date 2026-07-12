import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [plugin()],
    define: {
        global: 'globalThis',
    },
    server: {
        port: 47062,
        proxy: {
            '/api': {
                target: 'https://localhost:7204',
                changeOrigin: true,
                secure: false,
            }
        }
    }
})