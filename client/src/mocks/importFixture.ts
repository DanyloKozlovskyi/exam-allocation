// Dependency-free mock-import facts shared by the MSW handlers and the Playwright e2e spec (Node can't import
// the handlers, which pull in `config.json`).

/** Valid rows of the mock participants import (the rest have errors and are skipped). */
export const MOCK_IMPORT_VALID_ROWS = 48;
