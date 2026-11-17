/* eslint-disable @typescript-eslint/no-magic-numbers -- in-memory fixture data for MSW mocks */
import { http, HttpResponse, type HttpHandler } from 'msw';
import {
  AdjacencyKind,
  ErrorCode,
  ExportFormat,
  ViolationKind,
  type AllocationDto,
  type AllocationRowDto,
  type AllocationRowDtoPagedDto,
  type AllocationSummaryDto,
  type MovesDto,
  type ParticipantBriefDto,
  type PinAllDto,
  type PinDto,
  type RoomDto,
  type RoomSeatingDto,
  type ValidationReportDto,
  type ViolationDto,
} from '../../api/dtos';
import { ApiQueryParam, apiRoutes } from '../../constants/apiRoutes';
import { getMockSessionCenters } from './centers';
import { mockSessionParticipants } from './participants';
import { buildAdjacency, buildSeats, findMockRoom, getMockRooms, mockApiUrl, mockGuid, MOCK_CENTERS, problem, type MockCenter } from './rooms';
import { storeMockPins } from './pins';
import { mockSessionExists, touchMockSessionInput } from './sessions';

// ---------- participants ----------

const SURNAMES = [
  'Шевченко',
  'Коваленко',
  'Бондаренко',
  'Ткаченко',
  'Кравченко',
  'Олійник',
  'Шевчук',
  'Поліщук',
  'Бойко',
  'Ткачук',
  'Мельник',
  'Лисенко',
  'Марченко',
  'Савченко',
  'Руденко',
  'Мороз',
  'Гончаренко',
  'Левченко',
  'Петренко',
  'Захарченко',
  'Павленко',
  'Іваненко',
  'Кузьменко',
  'Литвиненко',
  'Онищенко',
  'Яковенко',
  'Гриценко',
  'Демченко',
  'Костенко',
  'Нестеренко',
];
const FIRST_NAMES = [
  'Олександр',
  'Марія',
  'Андрій',
  'Софія',
  'Дмитро',
  'Анастасія',
  'Максим',
  'Вікторія',
  'Іван',
  'Дарина',
  'Богдан',
  'Ольга',
  'Тарас',
  'Катерина',
  'Назар',
  'Юлія',
];
const MIDDLE_NAMES = ['Олександрович', 'Іванівна', 'Петрович', 'Миколаївна', 'Андрійович', 'Сергіївна', 'Васильович', 'Володимирівна'];
const GROUPS = ['11-А', '11-Б', '11-В', '10-А', '10-Б', '10-В'];
const SCHOOLS = ['Ліцей №142', 'Гімназія №1', 'ЗОШ №25', 'Ліцей «Інтелект»'];
export const MOCK_PARTICIPANT_COUNT = 72;

export const MOCK_PARTICIPANTS: ParticipantBriefDto[] = Array.from({ length: MOCK_PARTICIPANT_COUNT }, (_, i) => ({
  id: `d0000000-0000-4000-8000-${(i + 1).toString(16).padStart(12, '0')}`,
  lastName: SURNAMES[i % SURNAMES.length],
  firstName: FIRST_NAMES[(i * 7) % FIRST_NAMES.length],
  middleName: MIDDLE_NAMES[(i * 3) % MIDDLE_NAMES.length],
  group: GROUPS[i % GROUPS.length],
  school: SCHOOLS[(i * 5) % SCHOOLS.length],
}));

// ---------- allocation context: whose centers and participants an allocation places ----------

interface AllocationContext {
  centers: MockCenter[];
  participants: ParticipantBriefDto[];
  participantById: Map<string, ParticipantBriefDto>;
}

const toContext = (centers: MockCenter[], participants: ParticipantBriefDto[]): AllocationContext => ({
  centers,
  participants,
  participantById: new Map(participants.map((participant) => [participant.id!, participant])),
});

/** Session-agnostic fixture context (feature tests use ad-hoc session ids): first two `MOCK_CENTERS`, `MOCK_PARTICIPANTS`. */
const FIXTURE_CONTEXT = toContext(MOCK_CENTERS.slice(0, 2), MOCK_PARTICIPANTS);

/** A session of the mock store: its active centers (with their rooms) and its participants, as the API would place them. */
const sessionContext = (sessionId: string): AllocationContext => {
  const centers = getMockSessionCenters(sessionId)
    .filter((center) => center.isActive && center.id)
    .map((center) => ({ id: center.id!, name: center.name ?? '', latitude: center.latitude ?? 0, longitude: center.longitude ?? 0 }));
  const participants = mockSessionParticipants(sessionId).map(({ id, lastName, firstName, middleName, group, school }) => ({
    id: id ?? undefined,
    lastName,
    firstName,
    middleName,
    group,
    school,
  }));
  return toContext(centers, participants);
};

const contextFor = (sessionId?: string) => (sessionId && mockSessionExists(sessionId) ? sessionContext(sessionId) : FIXTURE_CONTEXT);

// ---------- allocations store ----------

interface Placement {
  roomId: string;
  seatKey: string;
}

interface MockAllocation {
  id: string;
  runId: string;
  sessionId?: string;
  index: number;
  context: AllocationContext;
  placements: Map<string, Placement>;
}

const allocations = new Map<string, MockAllocation>();

/** Rooms an allocation may use: active rooms of its context's centers. */
const allocationRooms = (context: AllocationContext) => context.centers.flatMap((center) => getMockRooms(center.id).filter((room) => room.isActive));

const seatsOf = (room: RoomDto) => buildSeats(room.layout ?? {});

/** Fills rooms seat by seat; `shift` rotates participants so alternatives differ. */
const generatePlacements = (shift: number, context: AllocationContext) => {
  const placements = new Map<string, Placement>();
  const slots = allocationRooms(context).flatMap((room) => seatsOf(room).map((seat) => ({ roomId: room.id!, seatKey: seat.seatKey! })));
  context.participants.forEach((participant, i) => {
    const slot = slots[(i * 2 + shift) % slots.length];
    if (slot && ![...placements.values()].some((p) => p.roomId === slot.roomId && p.seatKey === slot.seatKey)) {
      placements.set(participant.id!, slot);
    } else {
      const free = slots.find((candidate) => ![...placements.values()].some((p) => p.roomId === candidate.roomId && p.seatKey === candidate.seatKey));
      if (free) placements.set(participant.id!, free);
    }
  });
  return placements;
};

/** Creates `count` allocations of a run; with a `sessionId` of the mock store they place that session's participants in its rooms. */
export const createMockAllocations = (runId: string, count: number, sessionId?: string) =>
  Array.from({ length: count }, (_, index) => {
    const context = contextFor(sessionId);
    const allocation: MockAllocation = { id: mockGuid('e0'), runId, sessionId, index, context, placements: generatePlacements(index * 3, context) };
    allocations.set(allocation.id, allocation);
    return allocation.id;
  });

export const getAllocationIdsForRun = (runId: string) =>
  [...allocations.values()]
    .filter((allocation) => allocation.runId === runId)
    .sort((a, b) => (a.index < 0 ? Number.MAX_SAFE_INTEGER : a.index) - (b.index < 0 ? Number.MAX_SAFE_INTEGER : b.index))
    .map((allocation) => allocation.id);

export const resetAllocationsMocks = () => allocations.clear();

// ---------- evaluation ----------

const VIOLATION_KINDS: AdjacencyKind[] = [AdjacencyKind.SameDesk, AdjacencyKind.Side];

const roomViolations = (room: RoomDto, placements: Map<string, Placement>, { participantById }: AllocationContext): ViolationDto[] => {
  const seats = seatsOf(room);
  const occupants = new Map<string, string[]>();
  placements.forEach((placement, participantId) => {
    if (placement.roomId === room.id) {
      occupants.set(placement.seatKey, [...(occupants.get(placement.seatKey) ?? []), participantId]);
    }
  });

  const violations: ViolationDto[] = [];
  occupants.forEach((ids, seatKey) => {
    if (ids.length > 1) {
      ids.forEach((participantId) => violations.push({ kind: ViolationKind.SeatOccupiedMoreThanOnce, isHard: true, participantId, roomId: room.id, seatKey }));
    }
    if (!seats.some((seat) => seat.seatKey === seatKey)) {
      ids.forEach((participantId) => violations.push({ kind: ViolationKind.SeatNotFound, isHard: true, participantId, roomId: room.id, seatKey }));
    }
  });

  buildAdjacency(room.layout ?? {}, seats)
    .filter((group) => group.kind && VIOLATION_KINDS.includes(group.kind))
    .flatMap((group) => group.edges ?? [])
    .forEach((edge) => {
      const a = seats[edge.a!].seatKey!;
      const b = seats[edge.b!].seatKey!;
      const pa = occupants.get(a)?.[0];
      const pb = occupants.get(b)?.[0];
      if (pa && pb && participantById.get(pa)?.group === participantById.get(pb)?.group) {
        violations.push({ kind: ViolationKind.AdjacencyViolated, isHard: false, participantId: pa, roomId: room.id, seatKey: a });
        violations.push({ kind: ViolationKind.AdjacencyViolated, isHard: false, participantId: pb, roomId: room.id, seatKey: b });
      }
    });
  return violations;
};

const toAllocationDto = (allocation: MockAllocation): AllocationDto => {
  const rooms = allocationRooms(allocation.context);
  const violations = rooms.flatMap((room) => roomViolations(room, allocation.placements, allocation.context));
  const centers = allocation.context.centers.map((center) => ({
    centerId: center.id,
    name: center.name,
    latitude: center.latitude,
    longitude: center.longitude,
    rooms: getMockRooms(center.id).map((room) => {
      const filled = [...allocation.placements.values()].filter((placement) => placement.roomId === room.id).length;
      return { roomId: room.id!, name: room.name, used: filled > 0, filled, capacity: room.capacity };
    }),
  }));
  const usedRooms = centers.flatMap((center) => center.rooms).filter((room) => room.used);
  return {
    id: allocation.id,
    runId: allocation.runId,
    index: allocation.index,
    objective: { roomsUsed: usedRooms.length, cost: usedRooms.length, softPenalty: violations.filter((violation) => !violation.isHard).length / 2, penaltyByRule: {} },
    centers,
  };
};

const toSummary = (allocation: MockAllocation): AllocationSummaryDto => {
  const dto = toAllocationDto(allocation);
  return { id: dto.id, index: dto.index, objective: dto.objective };
};

const applyMoves = (allocation: MockAllocation, moves: MovesDto) => {
  const placements = new Map(allocation.placements);
  (moves.moves ?? []).forEach((move) => {
    if (move.participantId && move.roomId && move.seatKey) {
      placements.set(move.participantId, { roomId: move.roomId, seatKey: move.seatKey });
    }
  });
  return placements;
};

const EXPORT_CONTENT_TYPE: Record<ExportFormat, string> = {
  [ExportFormat.Xlsx]: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  [ExportFormat.Xml]: 'application/xml',
  [ExportFormat.Csv]: 'text/csv',
  [ExportFormat.Pdf]: 'application/pdf',
};

const notFound = () => problem(404, ErrorCode.AllocationNotFound, 'Allocation not found');

export const allocationsHandlers: HttpHandler[] = [
  http.get(mockApiUrl(apiRoutes.allocations.byRun(':runId')), ({ params }) =>
    HttpResponse.json(getAllocationIdsForRun(String(params.runId)).map((id) => toSummary(allocations.get(id)!)))
  ),
  http.get(mockApiUrl(apiRoutes.allocations.byId(':allocationId')), ({ params }) => {
    const allocation = allocations.get(String(params.allocationId));
    return allocation ? HttpResponse.json(toAllocationDto(allocation)) : notFound();
  }),
  http.get(mockApiUrl(apiRoutes.allocations.roomSeating(':allocationId', ':roomId')), ({ params }) => {
    const allocation = allocations.get(String(params.allocationId));
    const room = findMockRoom(String(params.roomId));
    if (!allocation || !room) {
      return notFound();
    }
    const violations = roomViolations(room, allocation.placements, allocation.context);
    const bySeat = new Map<string, string>();
    allocation.placements.forEach((placement, participantId) => placement.roomId === room.id && bySeat.set(placement.seatKey, participantId));
    const seating: RoomSeatingDto = {
      room,
      seats: seatsOf(room).map((seat) => {
        const participantId = bySeat.get(seat.seatKey!);
        return {
          seatKey: seat.seatKey,
          participant: participantId ? allocation.context.participantById.get(participantId) : undefined,
          violations: violations.filter((violation) => violation.seatKey === seat.seatKey),
        };
      }),
    };
    return HttpResponse.json(seating);
  }),
  http.get(mockApiUrl(apiRoutes.allocations.participants(':allocationId')), ({ params, request }) => {
    const allocation = allocations.get(String(params.allocationId));
    if (!allocation) {
      return notFound();
    }
    const url = new URL(request.url);
    const search = (url.searchParams.get(ApiQueryParam.Search) ?? '').toLocaleLowerCase();
    const page = Number(url.searchParams.get(ApiQueryParam.Page) ?? 1);
    const pageSize = Number(url.searchParams.get(ApiQueryParam.PageSize) ?? 50);
    const rows: AllocationRowDto[] = allocation.context.participants
      .filter((participant) => [participant.lastName, participant.firstName, participant.group].some((value) => value?.toLocaleLowerCase().includes(search)))
      .map((participant) => {
        const placement = allocation.placements.get(participant.id!);
        const room = placement ? findMockRoom(placement.roomId) : undefined;
        const center = allocation.context.centers.find((item) => item.id === room?.centerId);
        return { participant, centerId: center?.id, centerName: center?.name, roomId: room?.id ?? undefined, roomName: room?.name, seatKey: placement?.seatKey };
      });
    const body: AllocationRowDtoPagedDto = { items: rows.slice((page - 1) * pageSize, page * pageSize), page, pageSize, total: rows.length };
    return HttpResponse.json(body);
  }),
  http.post(mockApiUrl(apiRoutes.allocations.validateMoves(':allocationId')), async ({ params, request }) => {
    const allocation = allocations.get(String(params.allocationId));
    if (!allocation) {
      return notFound();
    }
    const placements = applyMoves(allocation, (await request.json()) as MovesDto);
    const violations = allocationRooms(allocation.context).flatMap((room) => roomViolations(room, placements, allocation.context));
    const report: ValidationReportDto = {
      isValid: !violations.some((violation) => violation.isHard),
      violations,
      softPenalty: violations.filter((violation) => !violation.isHard).length / 2,
    };
    return HttpResponse.json(report);
  }),
  http.post(mockApiUrl(apiRoutes.allocations.applyMoves(':allocationId')), async ({ params, request }) => {
    const allocation = allocations.get(String(params.allocationId));
    if (!allocation) {
      return notFound();
    }
    const moves = (await request.json()) as MovesDto;
    const placements = applyMoves(allocation, moves);
    const hard = allocationRooms(allocation.context)
      .flatMap((room) => roomViolations(room, placements, allocation.context))
      .some((violation) => violation.isHard);
    if (hard && !moves.force) {
      return problem(409, ErrorCode.MoveViolatesHardRule, 'Move violates a hard rule');
    }
    const manual: MockAllocation = { ...allocation, id: mockGuid('e0'), index: -1, placements };
    allocations.set(manual.id, manual);
    return HttpResponse.json(toAllocationDto(manual));
  }),
  http.post(mockApiUrl(apiRoutes.pins.pinAll(':allocationId')), async ({ params, request }) => {
    const allocation = allocations.get(String(params.allocationId));
    if (!allocation) {
      return notFound();
    }
    const { roomIds } = (await request.json()) as PinAllDto;
    const pins: PinDto[] = [...allocation.placements.entries()]
      .filter(([, placement]) => !roomIds || roomIds.includes(placement.roomId))
      .map(([participantId, placement]) => ({ id: mockGuid('f0'), participantId, roomId: placement.roomId, seatKey: placement.seatKey }));
    // Pins are solver input: the runs computed before them become stale.
    if (allocation.sessionId) {
      storeMockPins(allocation.sessionId, pins);
    }
    touchMockSessionInput(allocation.sessionId);
    return HttpResponse.json(pins);
  }),
  http.get(mockApiUrl(apiRoutes.exports.export(':allocationId')), ({ request }) => {
    const format = (new URL(request.url).searchParams.get(ApiQueryParam.Format) as ExportFormat | null) ?? ExportFormat.Csv;
    return new HttpResponse('mock export', { headers: { 'Content-Type': EXPORT_CONTENT_TYPE[format] ?? EXPORT_CONTENT_TYPE[ExportFormat.Csv] } });
  }),
];
