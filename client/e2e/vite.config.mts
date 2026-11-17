import { defineConfig, mergeConfig } from 'vite';
import baseConfig from '../vite.config.mts';

/**
 * Vite config of the e2e projects: the regular config (which already applies `VITE_USE_MOCKS` /
 * `VITE_API_URL` overrides) plus a separate dependency cache per dev server (`E2E_TARGET`),
 * so both projects can run side by side.
 */
export default mergeConfig(
  baseConfig,
  defineConfig({
    cacheDir: `e2e/.cache/vite-${process.env.E2E_TARGET ?? 'default'}`,
  })
);
