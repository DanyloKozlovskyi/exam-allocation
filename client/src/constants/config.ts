import rawConfig from '../config.json';

export interface AppConfig {
  apiUrl: string;
  googleMapsApiKey: string;
  googleMapsMapId: string;
  useMocks: boolean;
}

/**
 * Build-time overrides of `config.json`, injected with Vite `define` by `e2e/vite.config.mts`
 * from `VITE_USE_MOCKS` / `VITE_API_URL` (the e2e projects run the client with and without MSW).
 * Absent in the regular dev server, the production build and Jest, where `config.json` applies as is.
 */
declare const __APP_CONFIG_OVERRIDES__: Partial<AppConfig> | undefined;

const overrides: Partial<AppConfig> = typeof __APP_CONFIG_OVERRIDES__ === 'undefined' ? {} : __APP_CONFIG_OVERRIDES__;

export const appConfig: AppConfig = { ...rawConfig, ...overrides };
