import { test as base, type Locator, type Page } from '@playwright/test';

/** Per-project options (playwright.config.ts → `projects[].use`). */
export interface E2eOptions {
  /** The client runs with MSW mocks (no API) — some steps assert mock-specific outcomes. */
  useMocks: boolean;
}

export const test = base.extend<E2eOptions>({
  useMocks: [false, { option: true }],
});

export { expect } from '@playwright/test';

const QA_ATTRIBUTE = 'data-qa';

/** Element by its `data-qa` test id (from the feature `TestId` enums). */
export const byQa = (scope: Page | Locator, id: string) => scope.locator(`[${QA_ATTRIBUTE}="${id}"]`);

/** Elements whose `data-qa` starts with the id (e.g. `${LayoutTestId.NavigationItem}-<key>` families). */
export const byQaPrefix = (scope: Page | Locator, id: string) => scope.locator(`[${QA_ATTRIBUTE}^="${id}"]`);

/** The `<input>` of an MUI text field: the test id sits either on the input itself or on its wrapper. */
export const inputByQa = (scope: Page | Locator, id: string) => scope.locator(`input[${QA_ATTRIBUTE}="${id}"], [${QA_ATTRIBUTE}="${id}"] input`).first();

/** `data-qa` of a drawer item / badge (`components/Layout/AppLayout.tsx`). */
export const navigationQa = (prefix: string, key: string) => `${prefix}-${key}`;

const GRID_ROW_SELECTOR = '.MuiDataGrid-row';
const GRID_CELL_SELECTOR = '.MuiDataGrid-cell';

/** Data attributes the app sets next to `data-qa` (status chip, transport indicator). */
export enum DataAttribute {
  Status = 'data-status',
  Transport = 'data-transport',
}

/** Cells of a DataGrid row. */
export const gridCells = (row: Locator) => row.locator(GRID_CELL_SELECTOR);

/** Data rows of an MUI X DataGrid inside the scope. */
export const gridRows = (scope: Locator) => scope.locator(GRID_ROW_SELECTOR);
