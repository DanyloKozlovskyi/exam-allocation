import { http, HttpResponse, type HttpHandler } from 'msw';
import { ErrorCode, type SessionDto, type SessionSummaryDto, type SessionTotalsDto } from '../../api/dtos';
import { apiRoutes } from '../../constants/apiRoutes';
import { appConfig } from '../../constants/config';

// ---------------------------------------------------------------------------------------------
// Shared helpers of the in-memory mock API (WS-E). This module is a leaf: other handler files
// import from it, never the other way round (feature stores register themselves below).
// ---------------------------------------------------------------------------------------------

export enum HttpStatus {
  Ok = 200,
  NoContent = 204,
  BadRequest = 400,
  NotFound = 404,
  Conflict = 409,
}

export enum MockPathParam {
  SessionId = 'sessionId',
  CenterId = 'centerId',
  ParticipantId = 'participantId',
  ImportId = 'importId',
}

export const pathParam = (param: MockPathParam) => `:${param}`;
export const mockUrl = (path: string) => `${appConfig.apiUrl}/${path}`;

export const problem = (status: HttpStatus, code: ErrorCode) => HttpResponse.json({ title: code, status, code }, { status });
export const noContent = () => new HttpResponse(null, { status: HttpStatus.NoContent });

const GENERATED_ID_PREFIX = '0193b000-0000-7000-8000-';
const GENERATED_ID_DIGITS = 12;
let generatedIdCounter = 0;

/** Deterministic UUID-shaped ids (no dependency on `crypto.randomUUID` in jsdom). */
export const newId = () => {
  generatedIdCounter += 1;
  return `${GENERATED_ID_PREFIX}${String(generatedIdCounter).padStart(GENERATED_ID_DIGITS, '0')}`;
};
export const nowIso = () => new Date().toISOString();

/** Feature stores (centers, participants, …) contribute to totals and react to resets/session deletion. */
export interface MockStoreContributor {
  totals: (sessionId: string) => Partial<SessionTotalsDto>;
  removeSession: (sessionId: string) => void;
  reset: () => void;
}

const contributors: MockStoreContributor[] = [];

export const registerMockStore = (contributor: MockStoreContributor) => {
  contributors.push(contributor);
};

// ---------------------------------------------------------------------------------------------
// Sessions fixtures
// ---------------------------------------------------------------------------------------------

export const MOCK_SESSION_KYIV_ID = '0193a000-0000-7000-8000-000000000001';
export const MOCK_SESSION_LVIV_ID = '0193a000-0000-7000-8000-000000000002';
export const MOCK_SESSION_EMPTY_ID = '0193a000-0000-7000-8000-000000000003';

const INITIAL_VERSION = 1;

const createSessionFixtures = (): SessionDto[] => [
  {
    id: MOCK_SESSION_KYIV_ID,
    name: 'НМТ 2026 — Київ, основна сесія',
    examDate: '2026-06-10',
    description: 'Основна сесія національного мультипредметного тесту для учасників з міста Києва.',
    createdAt: '2026-03-01T09:00:00Z',
    updatedAt: '2026-04-12T14:30:00Z',
    version: INITIAL_VERSION,
  },
  {
    id: MOCK_SESSION_LVIV_ID,
    name: 'НМТ 2026 — Львів, додаткова сесія',
    examDate: '2026-07-15',
    description: 'Додаткова сесія для учасників Львівської області.',
    createdAt: '2026-03-05T10:00:00Z',
    updatedAt: '2026-04-20T08:15:00Z',
    version: INITIAL_VERSION,
  },
  {
    id: MOCK_SESSION_EMPTY_ID,
    name: 'Пробне тестування 2026',
    examDate: null,
    description: null,
    createdAt: '2026-04-01T12:00:00Z',
    updatedAt: '2026-04-01T12:00:00Z',
    version: INITIAL_VERSION,
  },
];

let sessions: SessionDto[] = createSessionFixtures();

export const mockSessionExists = (sessionId: string) => sessions.some((session) => session.id === sessionId);

/** Restores every registered mock store to its fixtures (used by tests). */
export const resetMockStore = () => {
  sessions = createSessionFixtures();
  contributors.forEach((contributor) => contributor.reset());
};

const toSummary = (session: SessionDto): SessionSummaryDto => ({
  id: session.id ?? '',
  name: session.name,
  examDate: session.examDate,
  updatedAt: session.updatedAt ?? nowIso(),
});

const computeTotals = (sessionId: string): SessionTotalsDto =>
  contributors.reduce<SessionTotalsDto>((totals, contributor) => ({ ...totals, ...contributor.totals(sessionId) }), {
    centers: 0,
    rooms: 0,
    totalCapacity: 0,
    accessibleCapacity: 0,
    participants: 0,
    needsAccessibleSeat: 0,
    groups: 0,
    rules: 0,
    isStale: false,
  });

const isNameTaken = (name: string | undefined, exceptId?: string | null) =>
  sessions.some((session) => session.id !== exceptId && session.name?.trim().toLowerCase() === name?.trim().toLowerCase());

const sessionPath = apiRoutes.sessions.byId(pathParam(MockPathParam.SessionId));

export const sessionsHandlers: HttpHandler[] = [
  http.get(mockUrl(apiRoutes.sessions.list()), () => HttpResponse.json(sessions.map(toSummary))),

  http.post(mockUrl(apiRoutes.sessions.create()), async ({ request }) => {
    const dto = (await request.json()) as SessionDto;
    if (!dto.name?.trim()) {
      return problem(HttpStatus.BadRequest, ErrorCode.ValidationFailed);
    }
    if (isNameTaken(dto.name)) {
      return problem(HttpStatus.Conflict, ErrorCode.SessionAlreadyExists);
    }
    const created: SessionDto = { ...dto, id: newId(), createdAt: nowIso(), updatedAt: nowIso(), version: INITIAL_VERSION };
    sessions = [...sessions, created];
    return HttpResponse.json(created);
  }),

  http.put(mockUrl(apiRoutes.sessions.edit()), async ({ request }) => {
    const dto = (await request.json()) as SessionDto;
    const existing = sessions.find((session) => session.id === dto.id);
    if (!existing) {
      return problem(HttpStatus.NotFound, ErrorCode.SessionNotFound);
    }
    if (existing.version !== dto.version) {
      return problem(HttpStatus.Conflict, ErrorCode.ConcurrencyConflict);
    }
    if (isNameTaken(dto.name, dto.id)) {
      return problem(HttpStatus.Conflict, ErrorCode.SessionAlreadyExists);
    }
    const updated: SessionDto = { ...existing, ...dto, updatedAt: nowIso(), version: (existing.version ?? INITIAL_VERSION) + 1 };
    sessions = sessions.map((session) => (session.id === dto.id ? updated : session));
    return HttpResponse.json(updated);
  }),

  http.get(mockUrl(apiRoutes.sessions.totals(pathParam(MockPathParam.SessionId))), ({ params }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    if (!mockSessionExists(sessionId)) {
      return problem(HttpStatus.NotFound, ErrorCode.SessionNotFound);
    }
    return HttpResponse.json(computeTotals(sessionId));
  }),

  http.get(mockUrl(sessionPath), ({ params }) => {
    const session = sessions.find((item) => item.id === params[MockPathParam.SessionId]);
    return session ? HttpResponse.json(session) : problem(HttpStatus.NotFound, ErrorCode.SessionNotFound);
  }),

  http.delete(mockUrl(sessionPath), ({ params }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    if (!mockSessionExists(sessionId)) {
      return problem(HttpStatus.NotFound, ErrorCode.SessionNotFound);
    }
    sessions = sessions.filter((session) => session.id !== sessionId);
    contributors.forEach((contributor) => contributor.removeSession(sessionId));
    return noContent();
  }),
];
