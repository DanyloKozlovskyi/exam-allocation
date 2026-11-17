// Named constants of the e2e suite (docs/10 §7: no magic strings / numbers).

/** Playwright project names (`--project=<name>`). */
export enum E2eProject {
  Mocks = 'mocks',
  RealApi = 'real-api',
}

/** Value of the `E2E_TARGET` env var passed to `e2e/vite.config.mts` (separate Vite caches). */
export enum E2eTarget {
  Mocks = 'mocks',
  Api = 'api',
}

export const HOST = 'localhost';
/** The API allows CORS only from http://localhost:3000, so the real-API client must use this port. */
export const REAL_API_CLIENT_PORT = 3000;
export const MOCKS_CLIENT_PORT = 3001;
export const API_PORT = 5080;

export const clientUrl = (port: number) => `http://${HOST}:${port}`;
export const API_URL = `http://${HOST}:${API_PORT}`;
export const API_HEALTH_URL = `${API_URL}/health`;

const SECOND_MS = 1_000;
const MINUTE_MS = 60 * SECOND_MS;

/** Generous bounds: the CI machine is shared and the solver runs twice in the real-API project. */
export const TEST_TIMEOUT_MS = 10 * MINUTE_MS;
export const EXPECT_TIMEOUT_MS = 20 * SECOND_MS;
export const ACTION_TIMEOUT_MS = 20 * SECOND_MS;
export const NAVIGATION_TIMEOUT_MS = 30 * SECOND_MS;
export const WEB_SERVER_TIMEOUT_MS = 3 * MINUTE_MS;
/** Upload + validation + commit of 300 rows. */
export const IMPORT_TIMEOUT_MS = MINUTE_MS;
/** One solve run (best + alternatives) on the small sample; ~5 s on an idle machine. */
export const SOLVE_TIMEOUT_MS = 4 * MINUTE_MS;

export const VIEWPORT = { width: 1440, height: 1000 };
