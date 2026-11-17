import { http, HttpResponse, type HttpHandler } from 'msw';
import { ErrorCode, type TestCenterDto } from '../../api/dtos';
import { apiRoutes } from '../../constants/apiRoutes';
import {
  HttpStatus,
  MOCK_SESSION_KYIV_ID,
  MOCK_SESSION_LVIV_ID,
  MockPathParam,
  mockSessionExists,
  mockUrl,
  newId,
  noContent,
  pathParam,
  problem,
  registerMockStore,
  touchMockSessionInput,
} from './sessions';

export const MOCK_CENTER_KNU_ID = '0193a100-0000-7000-8000-000000000001';
export const MOCK_CENTER_KPI_ID = '0193a100-0000-7000-8000-000000000002';
export const MOCK_CENTER_LYCEUM_ID = '0193a100-0000-7000-8000-000000000003';
export const MOCK_CENTER_LNU_ID = '0193a100-0000-7000-8000-000000000004';
export const MOCK_CENTER_LP_ID = '0193a100-0000-7000-8000-000000000005';

const INITIAL_VERSION = 1;

const createCenterFixtures = (): TestCenterDto[] => [
  {
    id: MOCK_CENTER_KNU_ID,
    sessionId: MOCK_SESSION_KYIV_ID,
    name: 'КНУ імені Тараса Шевченка, корпус 1',
    address: 'вул. Володимирська, 60, Київ',
    latitude: 50.4418,
    longitude: 30.5112,
    googlePlaceId: null,
    costPerRoom: 1.5,
    isActive: true,
    roomCount: 12,
    capacity: 360,
    version: INITIAL_VERSION,
  },
  {
    id: MOCK_CENTER_KPI_ID,
    sessionId: MOCK_SESSION_KYIV_ID,
    name: 'КПІ імені Ігоря Сікорського, корпус 7',
    address: 'просп. Берестейський, 37, Київ',
    latitude: 50.4488,
    longitude: 30.4572,
    googlePlaceId: null,
    costPerRoom: 1,
    isActive: true,
    roomCount: 10,
    capacity: 300,
    version: INITIAL_VERSION,
  },
  {
    id: MOCK_CENTER_LYCEUM_ID,
    sessionId: MOCK_SESSION_KYIV_ID,
    name: 'Ліцей № 171 «Лідер»',
    address: 'вул. Підвисоцького, 4, Київ',
    latitude: 50.422,
    longitude: 30.539,
    googlePlaceId: null,
    costPerRoom: null,
    isActive: false,
    roomCount: 6,
    capacity: 150,
    version: INITIAL_VERSION,
  },
  {
    id: MOCK_CENTER_LNU_ID,
    sessionId: MOCK_SESSION_LVIV_ID,
    name: 'ЛНУ імені Івана Франка',
    address: 'вул. Університетська, 1, Львів',
    latitude: 49.8406,
    longitude: 24.0226,
    googlePlaceId: null,
    costPerRoom: 1,
    isActive: true,
    roomCount: 8,
    capacity: 240,
    version: INITIAL_VERSION,
  },
  {
    id: MOCK_CENTER_LP_ID,
    sessionId: MOCK_SESSION_LVIV_ID,
    name: 'НУ «Львівська політехніка», головний корпус',
    address: 'вул. Степана Бандери, 12, Львів',
    latitude: 49.8357,
    longitude: 24.0144,
    googlePlaceId: null,
    costPerRoom: 1.2,
    isActive: true,
    roomCount: 9,
    capacity: 270,
    version: INITIAL_VERSION,
  },
];

/** Fixture centers as created (their `roomCount`/`capacity` describe the rooms the rooms store seeds for them). */
export const MOCK_CENTER_FIXTURES: readonly TestCenterDto[] = createCenterFixtures();

let centers: TestCenterDto[] = createCenterFixtures();

/** Room statistics of a center, computed by the rooms store (`mocks/handlers/rooms.ts`). */
export interface MockCenterRoomStats {
  roomCount: number;
  activeRooms: number;
  /** Seats of active rooms. */
  capacity: number;
  /** Accessible seats of active rooms. */
  accessibleCapacity: number;
}

interface CenterRoomStatsProvider {
  (centerId: string): MockCenterRoomStats;
}

let roomStatsProvider: CenterRoomStatsProvider | null = null;

/** The rooms store registers itself here, so center counts and session totals follow the rooms that exist. */
export const registerCenterRoomStats = (provider: CenterRoomStatsProvider) => {
  roomStatsProvider = provider;
};

const withRoomStats = (center: TestCenterDto): TestCenterDto => {
  if (!roomStatsProvider || !center.id) {
    return center;
  }
  const stats = roomStatsProvider(center.id);
  return { ...center, roomCount: stats.roomCount, capacity: stats.capacity };
};

const sessionCenters = (sessionId: string) => centers.filter((center) => center.sessionId === sessionId);

export const getMockSessionCenters = (sessionId: string) => sessionCenters(sessionId).map(withRoomStats);

export const findMockCenter = (centerId: string) => centers.find((center) => center.id === centerId);

export const isMockFixtureCenter = (centerId: string) => MOCK_CENTER_FIXTURES.some((center) => center.id === centerId);

registerMockStore({
  totals: (sessionId) => {
    const active = sessionCenters(sessionId).filter((center) => center.isActive);
    const stats = active.map((center) =>
      roomStatsProvider && center.id
        ? roomStatsProvider(center.id)
        : { roomCount: center.roomCount ?? 0, activeRooms: center.roomCount ?? 0, capacity: center.capacity ?? 0, accessibleCapacity: 0 }
    );
    return {
      centers: sessionCenters(sessionId).length,
      rooms: stats.reduce((sum, item) => sum + item.activeRooms, 0),
      totalCapacity: stats.reduce((sum, item) => sum + item.capacity, 0),
      accessibleCapacity: stats.reduce((sum, item) => sum + item.accessibleCapacity, 0),
    };
  },
  removeSession: (sessionId) => {
    centers = centers.filter((center) => center.sessionId !== sessionId);
  },
  reset: () => {
    centers = createCenterFixtures();
  },
});

const isNameTaken = (sessionId: string, name: string | undefined, exceptId?: string | null) =>
  sessionCenters(sessionId).some((center) => center.id !== exceptId && center.name?.trim().toLowerCase() === name?.trim().toLowerCase());

const sessionIdParam = pathParam(MockPathParam.SessionId);
const centerPath = apiRoutes.centers.byId(sessionIdParam, pathParam(MockPathParam.CenterId));

export const centersHandlers: HttpHandler[] = [
  http.get(mockUrl(apiRoutes.centers.list(sessionIdParam)), ({ params }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    if (!mockSessionExists(sessionId)) {
      return problem(HttpStatus.NotFound, ErrorCode.SessionNotFound);
    }
    return HttpResponse.json(getMockSessionCenters(sessionId));
  }),

  http.post(mockUrl(apiRoutes.centers.create(sessionIdParam)), async ({ params, request }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    const dto = (await request.json()) as TestCenterDto;
    if (!mockSessionExists(sessionId)) {
      return problem(HttpStatus.NotFound, ErrorCode.SessionNotFound);
    }
    if (!dto.name?.trim()) {
      return problem(HttpStatus.BadRequest, ErrorCode.ValidationFailed);
    }
    if (isNameTaken(sessionId, dto.name)) {
      return problem(HttpStatus.Conflict, ErrorCode.TestCenterAlreadyExists);
    }
    const created: TestCenterDto = { ...dto, id: newId(), sessionId, roomCount: 0, capacity: 0, version: INITIAL_VERSION };
    centers = [...centers, created];
    return HttpResponse.json(created);
  }),

  http.put(mockUrl(apiRoutes.centers.edit(sessionIdParam)), async ({ params, request }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    const dto = (await request.json()) as TestCenterDto;
    const existing = sessionCenters(sessionId).find((center) => center.id === dto.id);
    if (!existing) {
      return problem(HttpStatus.NotFound, ErrorCode.TestCenterNotFound);
    }
    if (existing.version !== dto.version) {
      return problem(HttpStatus.Conflict, ErrorCode.ConcurrencyConflict);
    }
    if (isNameTaken(sessionId, dto.name, dto.id)) {
      return problem(HttpStatus.Conflict, ErrorCode.TestCenterAlreadyExists);
    }
    const updated: TestCenterDto = {
      ...existing,
      ...dto,
      sessionId,
      roomCount: existing.roomCount,
      capacity: existing.capacity,
      version: (existing.version ?? INITIAL_VERSION) + 1,
    };
    centers = centers.map((center) => (center.id === dto.id ? updated : center));
    if (existing.isActive !== updated.isActive) {
      touchMockSessionInput(sessionId);
    }
    return HttpResponse.json(withRoomStats(updated));
  }),

  http.get(mockUrl(centerPath), ({ params }) => {
    const center = sessionCenters(String(params[MockPathParam.SessionId])).find((item) => item.id === params[MockPathParam.CenterId]);
    return center ? HttpResponse.json(withRoomStats(center)) : problem(HttpStatus.NotFound, ErrorCode.TestCenterNotFound);
  }),

  http.delete(mockUrl(centerPath), ({ params }) => {
    const centerId = String(params[MockPathParam.CenterId]);
    if (!centers.some((center) => center.id === centerId)) {
      return problem(HttpStatus.NotFound, ErrorCode.TestCenterNotFound);
    }
    touchMockSessionInput(findMockCenter(centerId)?.sessionId);
    centers = centers.filter((center) => center.id !== centerId);
    return noContent();
  }),
];
