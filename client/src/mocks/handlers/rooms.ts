/* eslint-disable @typescript-eslint/no-magic-numbers -- in-memory fixture data for MSW mocks */
import { http, HttpResponse, type HttpHandler } from 'msw';
import {
  AdjacencyKind,
  ErrorCode,
  type AdjacencyEdgesDto,
  type CreateRoomsBulkDto,
  type RoomDto,
  type RoomLayoutDto,
  type RoomLayoutPreviewDto,
  type SeatDto,
} from '../../api/dtos';
import { appConfig } from '../../constants/config';
import { apiRoutes } from '../../constants/apiRoutes';

// ---------- shared mock helpers (used by the other WS-F handler files) ----------

/** Absolute URL the axios instance calls for an `apiRoutes.*` path (route params as `:name`). */
export const mockApiUrl = (path: string) => `${appConfig.apiUrl}/${path}`;

let guidCounter = 0x1000;
/** Deterministic-looking v4 GUIDs for fixtures. */
export const mockGuid = (prefix: string) => {
  guidCounter += 1;
  return `${prefix.padEnd(8, '0').slice(0, 8)}-0000-4000-8000-${guidCounter.toString(16).padStart(12, '0')}`;
};

export const problem = (status: number, code: ErrorCode, title: string) => HttpResponse.json({ title, status, code }, { status });

// ---------- seats & adjacency (mirrors docs/03 "Seat adjacency") ----------

const seatKey = (row: number, column: number, seat: number) => `r${row}c${column}s${seat}`;

export const buildSeats = (layout: RoomLayoutDto): SeatDto[] => {
  const disabled = new Set(layout.disabledSeats ?? []);
  const accessible = new Set(layout.accessibleSeats ?? []);
  const seats: SeatDto[] = [];
  for (let row = 0; row < (layout.rows ?? 0); row++) {
    for (let column = 0; column < (layout.columns ?? 0); column++) {
      for (let seatIndex = 0; seatIndex < (layout.seatsPerDesk ?? 0); seatIndex++) {
        const key = seatKey(row, column, seatIndex);
        if (!disabled.has(key)) {
          seats.push({ seatKey: key, row, column, seatIndex, isAccessible: accessible.has(key) });
        }
      }
    }
  }

  return seats;
};

const kindOf = (a: SeatDto, b: SeatDto, aisles: Set<number>): AdjacencyKind | null => {
  const dr = Math.abs(a.row! - b.row!);
  const dc = Math.abs(a.column! - b.column!);
  const aisleBetween = dc === 1 && aisles.has(Math.min(a.column!, b.column!));
  if (dr === 0 && dc === 0) {
    return AdjacencyKind.SameDesk;
  }
  if (dr === 0 && dc === 1) {
    return aisleBetween ? AdjacencyKind.SideAcrossAisle : AdjacencyKind.Side;
  }
  if (dc === 0 && dr === 1) {
    return AdjacencyKind.FrontBack;
  }
  if (dr === 1 && dc === 1 && !aisleBetween) {
    return AdjacencyKind.Diagonal;
  }
  return null;
};

export const buildAdjacency = (layout: RoomLayoutDto, seats: SeatDto[]): AdjacencyEdgesDto[] => {
  const aisles = new Set(layout.aislesAfterColumns ?? []);
  const byKind = new Map<AdjacencyKind, AdjacencyEdgesDto>(Object.values(AdjacencyKind).map((kind) => [kind, { kind, edges: [] }]));
  seats.forEach((a, i) =>
    seats.slice(i + 1).forEach((b, offset) => {
      const kind = kindOf(a, b, aisles);
      if (kind) {
        byKind.get(kind)!.edges!.push({ a: i, b: i + 1 + offset });
      }
    })
  );
  return [...byKind.values()];
};

const withDerived = (room: RoomDto): RoomDto => {
  const seats = buildSeats(room.layout ?? {});
  return { ...room, seats, capacity: seats.length, accessibleCapacity: seats.filter((seat) => seat.isAccessible).length };
};

// ---------- fixtures ----------

export interface MockCenter {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}

export const MOCK_CENTERS: MockCenter[] = [
  { id: 'c0000001-0000-4000-8000-000000000001', name: 'Київський ліцей №142', latitude: 50.4501, longitude: 30.5234 },
  { id: 'c0000001-0000-4000-8000-000000000002', name: 'Гімназія №1 «Печерська»', latitude: 50.4268, longitude: 30.5396 },
  { id: 'c0000001-0000-4000-8000-000000000003', name: 'НТУУ «КПІ ім. Ігоря Сікорського», корпус 7', latitude: 50.4486, longitude: 30.4572 },
];

const standardLayout = (rows: number, columns: number, aisles: number[], accessibleSeats: string[] = [], disabledSeats: string[] = []): RoomLayoutDto => ({
  rows,
  columns,
  seatsPerDesk: 2,
  aislesAfterColumns: aisles,
  accessibleSeats,
  disabledSeats,
});

const seedRooms = (centerId: string): RoomDto[] => {
  const room = (name: string, layout: RoomLayoutDto, costOverride: number | null = null, isActive = true) =>
    withDerived({ id: mockGuid('a0'), centerId, name, layout, costOverride, isActive, version: 1 });

  switch (centerId) {
    case MOCK_CENTERS[0].id:
      return [
        room('Ауд. 101', standardLayout(5, 3, [0, 1], ['r0c0s0'])),
        room('Ауд. 102', standardLayout(5, 3, [0, 1])),
        room('Ауд. 214', standardLayout(6, 4, [1], ['r0c0s0', 'r0c0s1'], ['r5c3s1'])),
      ];
    case MOCK_CENTERS[1].id:
      return [room('Актова зала', { ...standardLayout(8, 6, [2]), seatsPerDesk: 3 }, 2.5), room('Кабінет історії', standardLayout(5, 3, [0, 1], ['r0c2s1']))];
    case MOCK_CENTERS[2].id:
      return [room('Ауд. 7-301', standardLayout(6, 5, [1, 3], ['r0c0s0'])), room('Ауд. 7-302', standardLayout(6, 5, [1, 3]), null, false)];
    default:
      return [room('Ауд. 1', standardLayout(5, 3, [0, 1], ['r0c0s0'])), room('Ауд. 2', standardLayout(5, 3, [0, 1]))];
  }
};

const roomsByCenter = new Map<string, RoomDto[]>();

export const getMockRooms = (centerId: string) => {
  if (!roomsByCenter.has(centerId)) {
    roomsByCenter.set(centerId, seedRooms(centerId));
  }
  return roomsByCenter.get(centerId)!;
};

export const findMockRoom = (roomId: string) => {
  MOCK_CENTERS.forEach((center) => getMockRooms(center.id));
  return [...roomsByCenter.values()].flat().find((room) => room.id === roomId);
};

export const resetRoomsMocks = () => roomsByCenter.clear();

const nameTaken = (centerId: string, name: string | undefined, exceptId?: string | null) => getMockRooms(centerId).some((room) => room.name === name && room.id !== exceptId);

// ---------- handlers ----------

export const roomsHandlers: HttpHandler[] = [
  http.get(mockApiUrl(apiRoutes.rooms.list(':centerId')), ({ params }) => HttpResponse.json(getMockRooms(String(params.centerId)).map((room) => ({ ...room, seats: null })))),
  http.post(mockApiUrl(apiRoutes.rooms.previewLayout()), async ({ request }) => {
    const layout = (await request.json()) as RoomLayoutDto;
    const seats = buildSeats(layout);
    const preview: RoomLayoutPreviewDto = {
      seats,
      capacity: seats.length,
      accessibleCapacity: seats.filter((seat) => seat.isAccessible).length,
      adjacency: buildAdjacency(layout, seats),
    };
    return HttpResponse.json(preview);
  }),
  http.post(mockApiUrl(apiRoutes.rooms.create(':centerId')), async ({ params, request }) => {
    const centerId = String(params.centerId);
    const dto = (await request.json()) as RoomDto;
    if (nameTaken(centerId, dto.name)) {
      return problem(409, ErrorCode.RoomAlreadyExists, 'Room already exists');
    }
    const room = withDerived({ ...dto, id: mockGuid('a0'), centerId, version: 1 });
    getMockRooms(centerId).push(room);
    return HttpResponse.json(room);
  }),
  http.post(mockApiUrl(apiRoutes.rooms.createBulk(':centerId')), async ({ params, request }) => {
    const centerId = String(params.centerId);
    const dto = (await request.json()) as CreateRoomsBulkDto;
    const names = dto.names ?? [];
    if (names.some((name) => nameTaken(centerId, name))) {
      return problem(409, ErrorCode.RoomAlreadyExists, 'Room already exists');
    }
    const created = names.map((name) => withDerived({ ...dto.template, name, id: mockGuid('a0'), centerId, version: 1 }));
    getMockRooms(centerId).push(...created);
    return HttpResponse.json(created);
  }),
  http.put(mockApiUrl(apiRoutes.rooms.edit(':centerId')), async ({ params, request }) => {
    const centerId = String(params.centerId);
    const dto = (await request.json()) as RoomDto;
    const rooms = getMockRooms(centerId);
    const index = rooms.findIndex((room) => room.id === dto.id);
    if (index < 0) {
      return problem(404, ErrorCode.RoomNotFound, 'Room not found');
    }
    if (rooms[index].version !== dto.version) {
      return problem(409, ErrorCode.ConcurrencyConflict, 'Concurrency conflict');
    }
    if (nameTaken(centerId, dto.name, dto.id)) {
      return problem(409, ErrorCode.RoomAlreadyExists, 'Room already exists');
    }
    rooms[index] = withDerived({ ...dto, centerId, version: (dto.version ?? 0) + 1 });
    return HttpResponse.json(rooms[index]);
  }),
  http.get(mockApiUrl(apiRoutes.rooms.byId(':centerId', ':roomId')), ({ params }) => {
    const room = getMockRooms(String(params.centerId)).find((item) => item.id === params.roomId);
    return room ? HttpResponse.json(room) : problem(404, ErrorCode.RoomNotFound, 'Room not found');
  }),
  http.delete(mockApiUrl(apiRoutes.rooms.byId(':centerId', ':roomId')), ({ params }) => {
    const rooms = getMockRooms(String(params.centerId));
    const index = rooms.findIndex((item) => item.id === params.roomId);
    if (index < 0) {
      return problem(404, ErrorCode.RoomNotFound, 'Room not found');
    }
    rooms.splice(index, 1);
    return new HttpResponse(null, { status: 204 });
  }),
];
