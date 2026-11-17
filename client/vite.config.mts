import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';

const DEV_SERVER_PORT = 3000;
const TRUE_VALUE = 'true';

/**
 * Optional build-time overrides of `src/config.json` (read by `src/constants/config.ts` via
 * `__APP_CONFIG_OVERRIDES__`): `VITE_USE_MOCKS=true npm run dev` runs the UI on MSW mocks,
 * `VITE_API_URL` points it at another API. Without them `config.json` applies unchanged.
 */
const env = process.env;
const overrides: Record<string, unknown> = {};
if (env.VITE_USE_MOCKS !== undefined) {
  overrides.useMocks = env.VITE_USE_MOCKS === TRUE_VALUE;
}
if (env.VITE_API_URL) {
  overrides.apiUrl = env.VITE_API_URL;
}

export default defineConfig({
  base: '/',
  plugins: [react()],
  define: { __APP_CONFIG_OVERRIDES__: JSON.stringify(overrides) },
  server: {
    port: DEV_SERVER_PORT,
  },
});
