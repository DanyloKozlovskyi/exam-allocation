import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';

const DEV_SERVER_PORT = 3000;

export default defineConfig({
  base: '/',
  plugins: [react()],
  server: {
    port: DEV_SERVER_PORT,
  },
});
