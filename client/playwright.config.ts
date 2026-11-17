import { defineConfig, devices, type PlaywrightTestConfig } from '@playwright/test';
import {
  ACTION_TIMEOUT_MS,
  API_HEALTH_URL,
  API_URL,
  clientUrl,
  E2eProject,
  E2eTarget,
  EXPECT_TIMEOUT_MS,
  MOCKS_CLIENT_PORT,
  NAVIGATION_TIMEOUT_MS,
  REAL_API_CLIENT_PORT,
  TEST_TIMEOUT_MS,
  VIEWPORT,
  WEB_SERVER_TIMEOUT_MS,
} from './e2e/support/constants';
import type { E2eOptions } from './e2e/support/fixtures';

/**
 * Two projects run the same happy path (docs/06, docs/smoke-test-phase2.md):
 * - `mocks`: the client with MSW (no API), dev server on :3001;
 * - `real-api`: the client against the API on :5080 (+ docker-compose Postgres), dev server on :3000.
 * Only the servers of the selected projects are started (`--project=…`); running servers are reused.
 * The API needs the database: `docker compose up -d db` (see docs/08-testing.md).
 */
const PROJECT_FLAG = '--project';
const CHROMIUM_EXECUTABLE_ENV = 'E2E_CHROMIUM_EXECUTABLE';

const selectedProjects = (): E2eProject[] => {
  const args = process.argv;
  const names = args.flatMap((arg, index) => {
    if (arg.startsWith(`${PROJECT_FLAG}=`)) {
      return [arg.slice(PROJECT_FLAG.length + 1)];
    }
    return arg === PROJECT_FLAG && args[index + 1] ? [args[index + 1]] : [];
  });
  const known = Object.values(E2eProject);
  const selected = names.filter((name): name is E2eProject => known.includes(name as E2eProject));
  return selected.length > 0 ? selected : known;
};

const viteCommand = (port: number) => `npx vite --config e2e/vite.config.mts --port ${port} --strictPort`;

const webServers: Record<E2eProject, NonNullable<PlaywrightTestConfig['webServer']>> = {
  [E2eProject.Mocks]: [
    {
      command: viteCommand(MOCKS_CLIENT_PORT),
      url: clientUrl(MOCKS_CLIENT_PORT),
      env: { VITE_USE_MOCKS: String(true), E2E_TARGET: E2eTarget.Mocks },
      reuseExistingServer: true,
      timeout: WEB_SERVER_TIMEOUT_MS,
    },
  ],
  [E2eProject.RealApi]: [
    {
      command: 'dotnet run --project ../api/src/Api',
      url: API_HEALTH_URL,
      reuseExistingServer: true,
      timeout: WEB_SERVER_TIMEOUT_MS,
    },
    {
      command: viteCommand(REAL_API_CLIENT_PORT),
      url: clientUrl(REAL_API_CLIENT_PORT),
      env: { VITE_USE_MOCKS: String(false), VITE_API_URL: API_URL, E2E_TARGET: E2eTarget.Api },
      reuseExistingServer: true,
      timeout: WEB_SERVER_TIMEOUT_MS,
    },
  ],
};

export default defineConfig<E2eOptions>({
  testDir: './e2e',
  outputDir: './test-results',
  timeout: TEST_TIMEOUT_MS,
  expect: { timeout: EXPECT_TIMEOUT_MS },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    ...devices['Desktop Chrome'],
    viewport: VIEWPORT,
    actionTimeout: ACTION_TIMEOUT_MS,
    navigationTimeout: NAVIGATION_TIMEOUT_MS,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    // Optional: a preinstalled Chromium when the bundled revision is not downloaded (docs/08-testing.md).
    launchOptions: { executablePath: process.env[CHROMIUM_EXECUTABLE_ENV] || undefined },
  },
  projects: [
    { name: E2eProject.Mocks, use: { baseURL: clientUrl(MOCKS_CLIENT_PORT), useMocks: true } },
    { name: E2eProject.RealApi, use: { baseURL: clientUrl(REAL_API_CLIENT_PORT), useMocks: false } },
  ],
  webServer: selectedProjects().flatMap((project) => webServers[project]),
});
