/* eslint-disable @typescript-eslint/no-magic-numbers -- in-memory fixture data for MSW mocks */
import { http, HttpResponse, type HttpHandler } from 'msw';
import { DiagnosticCode, DiagnosticSeverity, ErrorCode, SolveRunStatus, type DiagnosticDto, type SolveOptionsDto, type SolveRunDto } from '../../api/dtos';
import { apiRoutes } from '../../constants/apiRoutes';
import { createMockAllocations, getAllocationIdsForRun, resetAllocationsMocks } from './allocations';
import { mockApiUrl, mockGuid, MOCK_CENTERS, problem } from './rooms';

/** Simulated run timeline (polling only — there is no SignalR hub on mocks). */
export const MOCK_RUN_QUEUED_MS = 800;
export const MOCK_RUN_DURATION_MS = 5000;
const HOUR_MS = 3_600_000;

interface MockRun {
  dto: SolveRunDto;
  startedAtMs: number;
  alternatives: number;
  /** Fixed final status for seeded runs; live runs are derived from elapsed time. */
  frozen: boolean;
}

const runs = new Map<string, MockRun[]>();

const PRECHECK_DIAGNOSTICS: DiagnosticDto[] = [
  {
    code: DiagnosticCode.AttributeMissing,
    severity: DiagnosticSeverity.Warning,
    message: 'У 3 учасників відсутнє значення атрибута «school» — вони не конфліктуватимуть ні з ким.',
    arguments: { AttributeKey: 'school', Count: '3' },
  },
];

const INFEASIBLE_DIAGNOSTICS: DiagnosticDto[] = [
  {
    code: DiagnosticCode.CapacityTotalExceeded,
    severity: DiagnosticSeverity.Error,
    message: 'Учасників: 412, доступних місць в активних аудиторіях: 380.',
    arguments: { Size: '412', Limit: '380', CenterId: MOCK_CENTERS[2].id },
  },
];

const iso = (ms: number) => new Date(ms).toISOString();

const seedRuns = (sessionId: string): MockRun[] => {
  const now = Date.now();
  const succeededId = mockGuid('90');
  createMockAllocations(succeededId, 3);
  const succeeded: MockRun = {
    frozen: true,
    alternatives: 2,
    startedAtMs: now - 2 * HOUR_MS,
    dto: {
      id: succeededId,
      sessionId,
      status: SolveRunStatus.Succeeded,
      createdAt: iso(now - 2 * HOUR_MS),
      startedAt: iso(now - 2 * HOUR_MS + 1000),
      finishedAt: iso(now - 2 * HOUR_MS + 14_000),
      stats: { precheckMs: 120, stage1Ms: 6200, stage2Ms: 4100, alternativesMs: 2900, feedbackIterations: 1, classes: 12, variables: 1840, constraints: 2310 },
      diagnostics: [],
      isStale: true,
    },
  };
  const infeasible: MockRun = {
    frozen: true,
    alternatives: 0,
    startedAtMs: now - 3 * HOUR_MS,
    dto: {
      id: mockGuid('90'),
      sessionId,
      status: SolveRunStatus.Infeasible,
      createdAt: iso(now - 3 * HOUR_MS),
      startedAt: iso(now - 3 * HOUR_MS + 500),
      finishedAt: iso(now - 3 * HOUR_MS + 2500),
      diagnostics: INFEASIBLE_DIAGNOSTICS,
      isStale: true,
    },
  };
  return [succeeded, infeasible];
};

export const getMockRuns = (sessionId: string) => {
  if (!runs.has(sessionId)) {
    runs.set(sessionId, seedRuns(sessionId));
  }
  return runs.get(sessionId)!;
};

export const resetSolveRunsMocks = () => {
  runs.clear();
  resetAllocationsMocks();
};

/** Advances a live run according to its elapsed time and returns the DTO. */
const snapshot = (run: MockRun): SolveRunDto => {
  if (!run.frozen) {
    const elapsed = Date.now() - run.startedAtMs;
    if (elapsed >= MOCK_RUN_DURATION_MS) {
      run.frozen = true;
      createMockAllocations(run.dto.id!, run.alternatives + 1);
      run.dto = {
        ...run.dto,
        status: SolveRunStatus.Succeeded,
        finishedAt: iso(run.startedAtMs + MOCK_RUN_DURATION_MS),
        stats: { classes: 12, variables: 1840, constraints: 2310 },
      };
    } else if (elapsed >= MOCK_RUN_QUEUED_MS) {
      run.dto = { ...run.dto, status: SolveRunStatus.Running, startedAt: run.dto.startedAt ?? iso(run.startedAtMs + MOCK_RUN_QUEUED_MS) };
    }
  }
  return { ...run.dto, allocationIds: getAllocationIdsForRun(run.dto.id!) };
};

const findRun = (runId: string) => [...runs.values()].flat().find((run) => run.dto.id === runId);

const isActive = (run: MockRun) => {
  const status = snapshot(run).status;
  return status === SolveRunStatus.Queued || status === SolveRunStatus.Running;
};

export const solveRunsHandlers: HttpHandler[] = [
  http.post(mockApiUrl(apiRoutes.solveRuns.precheck(':sessionId')), () => HttpResponse.json(PRECHECK_DIAGNOSTICS)),
  http.get(mockApiUrl(apiRoutes.solveRuns.list(':sessionId')), ({ params }) =>
    HttpResponse.json(
      getMockRuns(String(params.sessionId))
        .map(snapshot)
        .sort((a, b) => (b.createdAt ?? '').localeCompare(a.createdAt ?? ''))
    )
  ),
  http.post(mockApiUrl(apiRoutes.solveRuns.create(':sessionId')), async ({ params, request }) => {
    const sessionId = String(params.sessionId);
    const options = (await request.json()) as SolveOptionsDto;
    const list = getMockRuns(sessionId);
    if (list.some(isActive)) {
      return problem(409, ErrorCode.SolveRunAlreadyActive, 'Solve run already active');
    }
    const now = Date.now();
    const run: MockRun = {
      frozen: false,
      alternatives: options.alternatives ?? 2,
      startedAtMs: now,
      dto: { id: mockGuid('90'), sessionId, status: SolveRunStatus.Queued, createdAt: iso(now), startedAt: null, finishedAt: null, diagnostics: [], isStale: false },
    };
    list.push(run);
    return HttpResponse.json(snapshot(run));
  }),
  http.get(mockApiUrl(apiRoutes.solveRuns.byId(':runId')), ({ params }) => {
    const run = findRun(String(params.runId));
    return run ? HttpResponse.json(snapshot(run)) : problem(404, ErrorCode.SolveRunNotFound, 'Solve run not found');
  }),
  http.post(mockApiUrl(apiRoutes.solveRuns.cancel(':runId')), ({ params }) => {
    const run = findRun(String(params.runId));
    if (!run) {
      return problem(404, ErrorCode.SolveRunNotFound, 'Solve run not found');
    }
    if (!isActive(run)) {
      return problem(409, ErrorCode.SolveRunNotCancellable, 'Solve run is not cancellable');
    }
    run.frozen = true;
    run.dto = { ...run.dto, status: SolveRunStatus.Cancelled, finishedAt: iso(Date.now()) };
    return new HttpResponse(null, { status: 202 });
  }),
];
