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
import { apiRoutes } from '../../constants/apiRoutes';
import { buildAdjacency, buildSeats, findMockRoom, getMockRooms, mockApiUrl, mockGuid, MOCK_CENTERS, problem } from './rooms';

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

const participantById = new Map(MOCK_PARTICIPANTS.map((participant) => [participant.id!, participant]));

// ---------- allocations store ----------

interface Placement {
  roomId: string;
  seatKey: string;
}

interface MockAllocation {
  id: string;
  runId: string;
  index: number;
  placements: Map<string, Placement>;
}

const allocations = new Map<string, MockAllocation>();

/** Rooms used by fixture allocations: active rooms of the first two mock centers. */
const allocationRooms = () => MOCK_CENTERS.slice(0, 2).flatMap((center) => getMockRooms(center.id).filter((room) => room.isActive));

const seatsOf = (room: RoomDto) => buildSeats(room.layout ?? {});

/** Fills rooms seat by seat; `shift` rotates participants so alternatives differ. */
const generatePlacements = (shift: number) => {
  const placements = new Map<string, Placement>();
  const slots = allocationRooms().flatMap((room) => seatsOf(room).map((seat) => ({ roomId: room.id!, seatKey: seat.seatKey! })));
  MOCK_PARTICIPANTS.forEach((participant, i) => {
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

export const createMockAllocations = (runId: string, count: number) =>
  Array.from({ length: count }, (_, index) => {
    const allocation: MockAllocation = { id: mockGuid('e0'), runId, index, placements: generatePlacements(index * 3) };
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

const roomViolations = (room: RoomDto, placements: Map<string, Placement>): ViolationDto[] => {
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
  const rooms = allocationRooms();
  const violations = rooms.flatMap((room) => roomViolations(room, allocation.placements));
  const centers = MOCK_CENTERS.slice(0, 2).map((center) => ({
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

const SEARCH_PARAM = 'search';
const PAGE_PARAM = 'page';
const PAGE_SIZE_PARAM = 'pageSize';
const FORMAT_PARAM = 'format';

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
    const violations = roomViolations(room, allocation.placements);
    const bySeat = new Map<string, string>();
    allocation.placements.forEach((placement, participantId) => placement.roomId === room.id && bySeat.set(placement.seatKey, participantId));
    const seating: RoomSeatingDto = {
      room,
      seats: seatsOf(room).map((seat) => {
        const participantId = bySeat.get(seat.seatKey!);
        return {
          seatKey: seat.seatKey,
          participant: participantId ? participantById.get(participantId) : undefined,
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
    const search = (url.searchParams.get(SEARCH_PARAM) ?? '').toLocaleLowerCase();
    const page = Number(url.searchParams.get(PAGE_PARAM) ?? 1);
    const pageSize = Number(url.searchParams.get(PAGE_SIZE_PARAM) ?? 50);
    const rows: AllocationRowDto[] = MOCK_PARTICIPANTS.filter((participant) =>
      [participant.lastName, participant.firstName, participant.group].some((value) => value?.toLocaleLowerCase().includes(search))
    ).map((participant) => {
      const placement = allocation.placements.get(participant.id!);
      const room = placement ? findMockRoom(placement.roomId) : undefined;
      const center = MOCK_CENTERS.find((item) => item.id === room?.centerId);
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
    const violations = allocationRooms().flatMap((room) => roomViolations(room, placements));
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
    const hard = allocationRooms()
      .flatMap((room) => roomViolations(room, placements))
      .some((violation) => violation.isHard);
    if (hard && !moves.force) {
      return problem(409, ErrorCode.MoveViolatesHardRule, 'Move violates a hard rule');
    }
    const manual: MockAllocation = { id: mockGuid('e0'), runId: allocation.runId, index: -1, placements };
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
    return HttpResponse.json(pins);
  }),
  http.get(mockApiUrl(apiRoutes.exports.export(':allocationId')), ({ request }) => {
    const format = (new URL(request.url).searchParams.get(FORMAT_PARAM) as ExportFormat | null) ?? ExportFormat.Csv;
    return new HttpResponse('mock export', { headers: { 'Content-Type': EXPORT_CONTENT_TYPE[format] ?? EXPORT_CONTENT_TYPE[ExportFormat.Csv] } });
  }),
];
