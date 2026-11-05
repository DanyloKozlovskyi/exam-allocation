import rawConfig from '../config.json';

export interface AppConfig {
  apiUrl: string;
  googleMapsApiKey: string;
  googleMapsMapId: string;
  useMocks: boolean;
}

export const appConfig: AppConfig = rawConfig;
