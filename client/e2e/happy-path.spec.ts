import { fileURLToPath } from 'node:url';
import type { Page } from '@playwright/test';
import { ExportFormat, RuleScope, RuleType, SolveRunStatus, SolveStage } from '../src/api/dtos';
import { NavigationItemKey } from '../src/components/Layout/navigation';
import { HubEvent } from '../src/constants/hub';
import { RoutePath } from '../src/constants/routes';
import { LayoutTestId } from '../src/constants/testIds';
import { AllocationsTestId, AllocationViewTab } from '../src/enums/Allocations';
import { CentersTestId } from '../src/enums/Centers';
import { ImportsTestId } from '../src/enums/Imports';
import { ParticipantsTestId } from '../src/enums/Participants';
import { RoomGridTestId, RoomsTestId, SeatState } from '../src/enums/Rooms';
import { BuiltInAttributeKey, RulesTestId } from '../src/enums/Rules';
import { SessionsTestId } from '../src/enums/Sessions';
import { SolveProgressTransport, SolveRunsTestId } from '../src/enums/SolveRuns';
import { SOLVE_STAGE_ORDER } from '../src/components/Global/SolveRuns/Common/constants';
import { MOCK_IMPORT_VALID_ROWS } from '../src/mocks/importFixture';
import { API_URL, IMPORT_TIMEOUT_MS, SOLVE_TIMEOUT_MS } from './support/constants';
import { byQa, byQaPrefix, DataAttribute, expect, gridCells, gridRows, inputByQa, navigationQa, test } from './support/fixtures';
import { format, pattern, uk } from './support/texts';

// docs/06 happy path as walked in docs/smoke-test-phase2.md. Navigation is click-only after the first
// page load: in the `mocks` project the MSW store lives in the page, so a reload would reset it.

const SAMPLE_PARTICIPANTS_FILE = fileURLToPath(new URL('../../samples/generated/small/participants.xlsx', import.meta.url));
/** `samples/generated/small`: 300 participants, 6 of them need an accessible seat (docs/08). */
const SAMPLE_PARTICIPANTS = 300;

const SESSION_NAME_PREFIX = 'E2E НМТ';
const CENTER = { name: 'Ліцей №142', address: 'м. Київ, вул. Хрещатик, 1', latitude: '50.4501', longitude: '30.5234' };
/** The smoke test adds a second center (without rooms) to check badges and the overview map. */
const CENTERS = [CENTER, { name: 'Гімназія №1', address: 'м. Київ, вул. Січових Стрільців, 3', latitude: '50.4600', longitude: '30.5000' }];
const ROOM = { name: 'Ауд. 101', rows: 6, columns: 5, seatsPerDesk: 2, copies: 8 };
const ROOM_CAPACITY = ROOM.rows * ROOM.columns * ROOM.seatsPerDesk;
/** Seats marked accessible in the wizard, as 1-based (row, desk, seat) like the seat labels. */
const ACCESSIBLE_SEATS = [
  { row: 1, column: 1, seat: 1 },
  { row: 1, column: ROOM.columns, seat: ROOM.seatsPerDesk },
];
const MAX_PER_ROOM = 4;
const EXPECTED_RULES = 3;
const EXPECTED_RUNS = 2;
const MIN_ALLOCATIONS = 2;
const HTTP_OK = 200;
const SIGNALR_RECORD_SEPARATOR = '\u001e';

interface HubMessage {
  arguments?: { stage?: SolveStage }[];
}

/** Consecutive duplicates removed (`Stage1, Stage1, Stage2` → `Stage1, Stage2`). */
const distinctRuns = <T>(values: T[]) => values.filter((value, index) => index === 0 || value !== values[index - 1]);

/** Rooms of the allocation that are used (unused ones are disabled list buttons). */
const USED_ROOM_SELECTOR = '[role="button"]:not([aria-disabled="true"])';
const STEP_NEXT = uk.common.actions.next;
const ROUTE_PARAM = /:[A-Za-z]+/g;
const ROUTE_SEGMENT = '[^/]+';

/** URL matcher for an app route pattern (`RoutePath`), ignoring the origin. */
const routeUrl = (path: RoutePath) => new RegExp(`${path.replace(ROUTE_PARAM, ROUTE_SEGMENT)}$`);

const navItem = (page: Page, key: NavigationItemKey) => byQa(page, navigationQa(LayoutTestId.NavigationItem, key));
const navBadge = (page: Page, key: NavigationItemKey) => byQa(page, navigationQa(LayoutTestId.NavigationBadge, key));
const next = (page: Page) => page.getByRole('button', { name: STEP_NEXT, exact: true }).click();

const pickOption = async (page: Page, combobox: string, option: string | RegExp) => {
  await page.getByRole('combobox', { name: combobox }).click();
  await page.getByRole('option', { name: option }).click();
};

const seatLabel = (seat: { row: number; column: number; seat: number }, state: SeatState) => format(uk.rooms.grid.seatLabel, { ...seat, state: uk.rooms.seatState[state] });

const createRule = async (page: Page, type: RuleType, configure?: () => Promise<void>) => {
  await byQa(page, RulesTestId.CreateButton).click();
  await pickOption(page, uk.rules.fields.type, uk.rules.ruleType[type]);
  await page.getByLabel(uk.rules.fields.hard, { exact: true }).check();
  await next(page);
  await configure?.();
  await next(page);
  await expect(byQa(page, RulesTestId.Summary)).toBeVisible();
  await page.getByRole('button', { name: uk.rules.actions.createRule }).click();
  await page.waitForURL(routeUrl(RoutePath.Rules));
};

const pickGroupAttribute = (page: Page) => pickOption(page, uk.rules.fields.attributeKey, new RegExp(`^${uk.rules.attribute[BuiltInAttributeKey.Group]}`));

/** Starts a run from the open run dialog and waits until it succeeds; returns the run view's allocation links count. */
const startRunAndWait = async (page: Page, useMocks: boolean) => {
  const dialog = byQa(page, SolveRunsTestId.RunDialog);
  await expect(dialog).toBeVisible();
  const submit = byQa(page, SolveRunsTestId.RunDialogSubmit);
  await expect(submit).toBeEnabled();
  await submit.click();
  await page.waitForURL(routeUrl(RoutePath.SolveRunView));
  await expect(byQa(page, SolveRunsTestId.StageStepper)).toBeVisible();
  const transport = useMocks ? SolveProgressTransport.Polling : SolveProgressTransport.Hub;
  await expect(byQa(page, SolveRunsTestId.TransportIndicator)).toHaveAttribute(DataAttribute.Transport, transport);
  await expect(byQa(page, SolveRunsTestId.StatusChip)).toHaveAttribute(DataAttribute.Status, SolveRunStatus.Succeeded, { timeout: SOLVE_TIMEOUT_MS });
  const links = byQa(page, SolveRunsTestId.AllocationLink);
  await expect(links.first()).toBeVisible();
  return links.count();
};

test('happy path: session → centers → rooms → import → rules → solve → results → manual move → pin all → re-run', async ({ page, useMocks }) => {
  const sessionName = `${SESSION_NAME_PREFIX} ${Date.now()}`;
  const expectedParticipants = useMocks ? MOCK_IMPORT_VALID_ROWS : SAMPLE_PARTICIPANTS;
  const hubEvents: string[] = [];
  const hubStages: SolveStage[] = [];
  page.on('websocket', (socket) =>
    socket.on('framereceived', (frame) => {
      const payload = String(frame.payload);
      Object.values(HubEvent)
        .filter((event) => payload.includes(`"target":"${event}"`))
        .forEach((event) => hubEvents.push(event));
      // SignalR JSON protocol: messages separated by the record separator; ProgressUpdated carries SolveProgressDto.
      payload
        .split(SIGNALR_RECORD_SEPARATOR)
        .filter((message) => message.includes(`"target":"${HubEvent.ProgressUpdated}"`))
        .forEach((message) => {
          const progress = (JSON.parse(message) as HubMessage).arguments?.[0];
          if (progress?.stage) hubStages.push(progress.stage);
        });
    })
  );

  // In the mocks project every API call must be answered by the MSW service worker, never by a running API.
  const leakedApiCalls: string[] = [];
  page.on('response', (response) => {
    if (useMocks && response.url().startsWith(API_URL) && !response.fromServiceWorker()) {
      leakedApiCalls.push(`${response.request().method()} ${response.url()}`);
    }
  });

  await test.step('create a session with the stepper', async () => {
    await page.goto(RoutePath.Sessions);
    await byQa(page, SessionsTestId.CreateButton).click();
    await inputByQa(page, SessionsTestId.NameField).fill(sessionName);
    await next(page);
    await expect(byQa(page, SessionsTestId.Review)).toContainText(sessionName);
    await page.getByRole('button', { name: uk.sessions.form.createAction }).click();
    await page.waitForURL(routeUrl(RoutePath.SessionView));
    await expect(byQa(page, SessionsTestId.Dashboard)).toBeVisible();
  });

  await test.step('add two centers from the map fallback coordinates form', async () => {
    await navItem(page, NavigationItemKey.Centers).click();
    await expect(byQa(page, CentersTestId.MapFallback)).toBeVisible();
    for (const [index, center] of CENTERS.entries()) {
      await inputByQa(page, CentersTestId.FallbackLatitude).fill(center.latitude);
      await inputByQa(page, CentersTestId.FallbackLongitude).fill(center.longitude);
      await byQa(page, CentersTestId.FallbackAdd).click();
      await inputByQa(page, CentersTestId.NameField).fill(center.name);
      await inputByQa(page, CentersTestId.AddressField).fill(center.address);
      await next(page);
      await expect(byQa(page, CentersTestId.Review)).toContainText(center.name);
      await page.getByRole('button', { name: uk.centers.form.createAction }).click();
      await page.waitForURL(routeUrl(RoutePath.Centers));
      await expect(gridRows(byQa(page, CentersTestId.List)).filter({ hasText: center.name })).toBeVisible();
      await expect(navBadge(page, NavigationItemKey.Centers)).toHaveText(String(index + 1));
    }
  });

  await test.step('create rooms with the wizard: accessible seats on the grid, duplicated via create-bulk', async () => {
    await byQa(gridRows(byQa(page, CentersTestId.List)).filter({ hasText: CENTER.name }), CentersTestId.RoomsLink).click();
    await page.waitForURL(routeUrl(RoutePath.Rooms));
    await byQa(page, RoomsTestId.CreateButton).click();
    await inputByQa(page, RoomsTestId.NameField).fill(ROOM.name);
    await inputByQa(page, RoomsTestId.RowsField).fill(String(ROOM.rows));
    await inputByQa(page, RoomsTestId.ColumnsField).fill(String(ROOM.columns));
    await next(page);
    await expect(page.getByText(format(uk.rooms.layout.previewCapacity, { capacity: ROOM_CAPACITY, accessible: 0 }))).toBeVisible();
    for (const seat of ACCESSIBLE_SEATS) {
      await page.getByRole('button', { name: seatLabel(seat, SeatState.Normal), exact: true }).click();
      await expect(page.getByRole('button', { name: seatLabel(seat, SeatState.Accessible), exact: true })).toBeVisible();
    }
    await expect(page.getByText(format(uk.rooms.layout.previewCapacity, { capacity: ROOM_CAPACITY, accessible: ACCESSIBLE_SEATS.length }))).toBeVisible();
    await expect(byQaPrefix(page, RoomGridTestId.Seat)).toHaveCount(ROOM_CAPACITY);
    await next(page);
    await inputByQa(page, RoomsTestId.DuplicateCountField).fill(String(ROOM.copies));
    await page.getByRole('button', { name: format(uk.rooms.actions.createRooms, { count: ROOM.copies }) }).click();
    await page.waitForURL(routeUrl(RoutePath.Rooms));
    await expect(gridRows(byQa(page, RoomsTestId.List))).toHaveCount(ROOM.copies);
    await expect(navBadge(page, NavigationItemKey.Rooms)).toHaveText(String(ROOM.copies));
  });

  await test.step('import participants from the sample xlsx', async () => {
    await navItem(page, NavigationItemKey.Participants).click();
    await byQa(page, ParticipantsTestId.ImportButton).click();
    await expect(byQa(page, ImportsTestId.Wizard)).toBeVisible();
    await byQa(page, ImportsTestId.FileInput).setInputFiles(SAMPLE_PARTICIPANTS_FILE);
    await next(page);
    await expect(byQa(page, ImportsTestId.SamplePreview)).toBeVisible();
    await next(page);
    await expect(byQaPrefix(page, ImportsTestId.MappingField).first()).toBeVisible();
    await next(page);
    const summary = byQa(page, ImportsTestId.ValidationSummary);
    await expect(summary).toContainText(format(uk.imports.validation.validRows, { count: expectedParticipants }), { timeout: IMPORT_TIMEOUT_MS });
    if (!useMocks) {
      await expect(summary).toContainText(format(uk.imports.validation.errorRows, { count: 0 }));
    }
    await next(page);
    await expect(byQa(page, ImportsTestId.ModeGroup)).toBeVisible();
    if (useMocks) {
      // The mock file has rows with errors (src/mocks/handlers/imports.ts): skip them.
      await inputByQa(page, ImportsTestId.SkipInvalid).check();
    }
    await next(page);
    await expect(byQa(page, ImportsTestId.CommitSummary)).toBeVisible();
    await page.getByRole('button', { name: uk.imports.wizard.commitAction }).click();
    await expect(byQa(page, ImportsTestId.Result)).toContainText(format(uk.imports.commit.inserted, { count: expectedParticipants }), { timeout: IMPORT_TIMEOUT_MS });
    await byQa(page, ImportsTestId.FinishButton).click();
    await page.waitForURL(routeUrl(RoutePath.Participants));
    await expect(gridRows(byQa(page, ParticipantsTestId.Grid)).first()).toBeVisible();
    await expect(navBadge(page, NavigationItemKey.Participants)).toHaveText(String(expectedParticipants));
  });

  await test.step('configure rules and run the precheck', async () => {
    await navItem(page, NavigationItemKey.Rules).click();
    await createRule(page, RuleType.SeparateByAttribute, async () => {
      await pickGroupAttribute(page);
      await pickOption(page, uk.rules.fields.scope, uk.rules.scope[RuleScope.Room]);
      await inputByQa(page, RulesTestId.MaxPerScopeField).fill(String(MAX_PER_ROOM));
    });
    await createRule(page, RuleType.NoAdjacentByAttribute, () => pickGroupAttribute(page));
    await createRule(page, RuleType.AccessibleSeating);
    await expect(gridRows(byQa(page, RulesTestId.List))).toHaveCount(EXPECTED_RULES);
    await expect(navBadge(page, NavigationItemKey.Rules)).toHaveText(String(EXPECTED_RULES));
    await byQa(page, RulesTestId.PrecheckButton).click();
    await expect(byQa(page, SolveRunsTestId.DiagnosticsPanel)).toContainText(uk.solveRuns.diagnostics.empty);
  });

  let allocationLinks = 0;
  await test.step('solve with live progress', async () => {
    await navItem(page, NavigationItemKey.SolveRuns).click();
    await byQa(page, SolveRunsTestId.RunButton).click();
    allocationLinks = await startRunAndWait(page, useMocks);
    expect(allocationLinks).toBeGreaterThanOrEqual(MIN_ALLOCATIONS);
    if (!useMocks) {
      expect(hubEvents).toContain(HubEvent.ProgressUpdated);
      expect(hubEvents).toContain(HubEvent.RunCompleted);
      // Stages arrive in pipeline order (docs/04 §8a): never backwards, ending with Done.
      const sequence = distinctRuns(hubStages);
      const positions = sequence.map((stage) => SOLVE_STAGE_ORDER.indexOf(stage));
      expect(positions).toEqual([...positions].sort((a, b) => a - b));
      expect(sequence.at(-1)).toBe(SolveStage.Done);
    }
  });

  await test.step('results: overview, alternatives, export, participants search', async () => {
    await byQa(page, SolveRunsTestId.AllocationLink).first().click();
    await page.waitForURL(routeUrl(RoutePath.AllocationView));
    await expect(byQa(page, AllocationsTestId.OverviewMap)).toBeVisible();
    await expect(byQa(page, AllocationsTestId.RoomFillBar).first()).toBeVisible();
    await expect(byQa(page, AllocationsTestId.AlternativeTabs).getByRole('tab')).toHaveCount(allocationLinks);

    await byQa(page, AllocationsTestId.ExportButton).click();
    const items = byQa(page, AllocationsTestId.ExportMenuItem);
    await expect(items).toHaveCount(Object.values(ExportFormat).length);
    const hrefs = await items.evaluateAll((links) => links.map((link) => link.getAttribute('href') ?? ''));
    // Fetched from the page, so the MSW service worker answers in the mocks project.
    const statuses = await page.evaluate((urls) => Promise.all(urls.map((url) => fetch(url).then((response) => response.status))), hrefs);
    expect(statuses).toEqual(hrefs.map(() => HTTP_OK));
    await page.keyboard.press('Escape');

    await page.getByRole('tab', { name: uk.allocations.tabs[AllocationViewTab.Participants] }).click();
    const rows = gridRows(byQa(page, AllocationsTestId.ParticipantsGrid));
    await expect(rows.first()).toBeVisible();
    const lastName = (await gridCells(rows.first()).first().innerText()).trim();
    await inputByQa(page, AllocationsTestId.SearchField).fill(lastName);
    await expect.poll(async () => (await rows.allInnerTexts()).every((text) => text.includes(lastName))).toBe(true);
    await expect(rows.first()).toContainText(lastName);
    await page.getByRole('tab', { name: uk.allocations.tabs[AllocationViewTab.Overview] }).click();
  });

  await test.step('room seating: manual move, validate, apply', async () => {
    await byQa(page, AllocationsTestId.RoomsList).locator(USED_ROOM_SELECTOR).first().click();
    await page.waitForURL(routeUrl(RoutePath.AllocationRoom));
    const roomUrl = page.url();
    const grid = byQa(page, AllocationsTestId.SeatingGrid);
    await expect(grid).toBeVisible();
    await expect(byQa(page, AllocationsTestId.ViolationLegend)).toBeVisible();
    await byQa(page, AllocationsTestId.MoveModeToggle).check();
    const occupied = grid.locator(`[data-state="${SeatState.Occupied}"]`);
    const free = grid.locator(`[data-state="${SeatState.Normal}"], [data-state="${SeatState.Accessible}"]`);
    await expect(occupied.first()).toBeVisible();
    await expect(free.first()).toBeVisible();
    await occupied.first().click();
    await free.last().click();
    await expect(byQa(page, AllocationsTestId.PendingMoves)).toContainText(format(uk.allocations.seating.pendingMoves, { count: 1 }));
    await byQa(page, AllocationsTestId.ValidateMovesButton).click();
    const report = byQa(page, AllocationsTestId.ValidationReport);
    await expect(report).toBeVisible();
    const isValid = pattern(uk.allocations.seating.reportValid).test(await report.innerText());
    await byQa(page, AllocationsTestId.ApplyMovesButton).click();
    if (!isValid) {
      // A move next to a classmate breaks the hard NoAdjacent rule: save it anyway (force).
      await page.getByRole('button', { name: uk.common.actions.confirm }).click();
    }
    await expect(page.getByText(uk.allocations.messages.applied)).toBeVisible();
    // The view switches to the manual version (a new allocation id, same room).
    await page.waitForURL((url) => url.href !== roomUrl && routeUrl(RoutePath.AllocationRoom).test(url.pathname));
  });

  await test.step('dashboard shows the last run without the stale warning', async () => {
    await navItem(page, NavigationItemKey.Overview).click();
    const lastRun = byQa(page, SessionsTestId.LastRun);
    await expect(lastRun).toContainText(uk.solveRuns.status[SolveRunStatus.Succeeded]);
    await expect(lastRun.getByRole('link', { name: uk.sessions.view.openResults })).toBeVisible();
    await expect(byQa(page, SessionsTestId.StaleWarning)).toHaveCount(0);
  });

  await test.step('pin all and re-run', async () => {
    await navItem(page, NavigationItemKey.Results).click();
    await page.waitForURL(routeUrl(RoutePath.AllocationView));
    await byQa(page, AllocationsTestId.PinAllButton).click();
    await expect(page.getByText(format(uk.allocations.messages.pinned, { count: expectedParticipants }))).toBeVisible();
    await startRunAndWait(page, useMocks);
  });

  await test.step('runs list marks the first run stale', async () => {
    await navItem(page, NavigationItemKey.SolveRuns).click();
    const runs = gridRows(byQa(page, SolveRunsTestId.List));
    await expect(runs).toHaveCount(EXPECTED_RUNS);
    await expect(byQa(runs.first(), SolveRunsTestId.StaleBadge)).toHaveCount(0);
    await expect(byQa(runs.last(), SolveRunsTestId.StaleBadge)).toBeVisible();
  });

  expect(leakedApiCalls).toEqual([]);
});
