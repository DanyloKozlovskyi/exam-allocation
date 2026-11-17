import { http, HttpResponse, type HttpHandler } from 'msw';
import { ErrorCode, ParticipantSortField, SortDirection, type AttributeInfoDto, type ParticipantDto, type ParticipantDtoPagedDto } from '../../api/dtos';
import { apiRoutes } from '../../constants/apiRoutes';
import { DEFAULT_PAGE_SIZE, FIRST_PAGE } from '../../constants/queryParams';
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

// Query string names of GET sessions/{id}/participants (docs/05 paging) — mock side only.
export enum ParticipantsQueryParam {
  Page = 'page',
  PageSize = 'pageSize',
  Search = 'search',
  SortBy = 'sortBy',
  SortDirection = 'sortDirection',
  Group = 'group',
  School = 'school',
  NeedsAccessibleSeat = 'needsAccessibleSeat',
}

export enum MockAttributeKey {
  Profile = 'profil',
  ForeignLanguage = 'inozemna_mova',
}

const INITIAL_VERSION = 1;
const KYIV_PARTICIPANTS = 120;
const LVIV_PARTICIPANTS = 45;
const ACCESSIBLE_EVERY = 23;
const NO_MIDDLE_NAME_EVERY = 17;
const MISSING_LANGUAGE_EVERY = 11;
const EXTERNAL_ID_DIGITS = 6;
const SAMPLE_VALUES_LIMIT = 5;
const BOOLEAN_TRUE = 'true';

const LAST_NAMES = [
  'Шевченко',
  'Коваленко',
  'Бондаренко',
  'Ткаченко',
  'Кравченко',
  'Олійник',
  'Шевчук',
  'Поліщук',
  'Бойко',
  'Мельник',
  'Лисенко',
  'Савченко',
  'Руденко',
  'Марченко',
  'Петренко',
  'Іваненко',
  'Гончаренко',
  'Литвиненко',
  'Мороз',
  'Павленко',
  'Кузьменко',
  'Захарчук',
];

interface FirstName {
  name: string;
  isFemale: boolean;
}

const FIRST_NAMES: FirstName[] = [
  { name: 'Олександр', isFemale: false },
  { name: 'Марія', isFemale: true },
  { name: 'Андрій', isFemale: false },
  { name: 'Анастасія', isFemale: true },
  { name: 'Дмитро', isFemale: false },
  { name: 'Софія', isFemale: true },
  { name: 'Максим', isFemale: false },
  { name: 'Вікторія', isFemale: true },
  { name: 'Іван', isFemale: false },
  { name: 'Дарина', isFemale: true },
  { name: 'Богдан', isFemale: false },
  { name: 'Ольга', isFemale: true },
  { name: 'Тарас', isFemale: false },
  { name: 'Катерина', isFemale: true },
];

interface FatherName {
  male: string;
  female: string;
}

const FATHER_NAMES: FatherName[] = [
  { male: 'Олександрович', female: 'Олександрівна' },
  { male: 'Миколайович', female: 'Миколаївна' },
  { male: 'Сергійович', female: 'Сергіївна' },
  { male: 'Володимирович', female: 'Володимирівна' },
  { male: 'Іванович', female: 'Іванівна' },
  { male: 'Петрович', female: 'Петрівна' },
  { male: 'Васильович', female: 'Василівна' },
];

const GROUPS = ['11-А', '11-Б', '11-В', '10-А', '10-Б'];
const KYIV_SCHOOLS = ['Ліцей № 171 «Лідер»', 'Гімназія № 153 імені О. С. Пушкіна', 'Ліцей № 100 «Поділ»', 'Природничо-науковий ліцей № 145'];
const LVIV_SCHOOLS = ['Львівський фізико-математичний ліцей', 'Академічна гімназія при НУ «Львівська політехніка»', 'Ліцей № 52 Львівської міської ради'];
const PROFILES = ['Математика', 'Історія України', 'Біологія', 'Фізика', 'Хімія'];
const LANGUAGES = ['Англійська', 'Німецька', 'Французька', 'Іспанська'];

const pick = <T>(values: T[], index: number): T => values[index % values.length];

const generateParticipants = (sessionId: string, count: number, schools: string[], externalPrefix: string): ParticipantDto[] =>
  Array.from({ length: count }, (_, index) => {
    const firstName = pick(FIRST_NAMES, index);
    const fatherName = pick(FATHER_NAMES, index + LAST_NAMES.length);
    const attributes: Record<string, string> = { [MockAttributeKey.Profile]: pick(PROFILES, index + GROUPS.length) };
    if ((index + 1) % MISSING_LANGUAGE_EVERY !== 0) {
      attributes[MockAttributeKey.ForeignLanguage] = pick(LANGUAGES, index);
    }
    return {
      id: newId(),
      sessionId,
      externalId: `${externalPrefix}-${String(index + 1).padStart(EXTERNAL_ID_DIGITS, '0')}`,
      lastName: pick(LAST_NAMES, index),
      firstName: firstName.name,
      middleName: (index + 1) % NO_MIDDLE_NAME_EVERY === 0 ? null : firstName.isFemale ? fatherName.female : fatherName.male,
      group: pick(GROUPS, index),
      school: pick(schools, index),
      needsAccessibleSeat: (index + 1) % ACCESSIBLE_EVERY === 0,
      attributes,
      notes: null,
      version: INITIAL_VERSION,
    };
  });

const createParticipantFixtures = (): ParticipantDto[] => [
  ...generateParticipants(MOCK_SESSION_KYIV_ID, KYIV_PARTICIPANTS, KYIV_SCHOOLS, 'KYIV'),
  ...generateParticipants(MOCK_SESSION_LVIV_ID, LVIV_PARTICIPANTS, LVIV_SCHOOLS, 'LVIV'),
];

let participants: ParticipantDto[] = createParticipantFixtures();

const sessionParticipants = (sessionId: string) => participants.filter((participant) => participant.sessionId === sessionId);

/** Used by the imports mock to commit rows. */
export const mockAddParticipants = (sessionId: string, items: ParticipantDto[]) => {
  participants = [...participants, ...items.map((item) => ({ ...item, id: newId(), sessionId, version: INITIAL_VERSION }))];
  touchMockSessionInput(sessionId);
};

export const mockRemoveSessionParticipants = (sessionId: string) => {
  participants = participants.filter((participant) => participant.sessionId !== sessionId);
  touchMockSessionInput(sessionId);
};

/** Participants of a session (allocation mocks place them). */
export const mockSessionParticipants = (sessionId: string) => sessionParticipants(sessionId);

export const mockParticipantsCount = (sessionId: string) => sessionParticipants(sessionId).length;

registerMockStore({
  totals: (sessionId) => {
    const items = sessionParticipants(sessionId);
    return {
      participants: items.length,
      needsAccessibleSeat: items.filter((participant) => participant.needsAccessibleSeat).length,
      groups: new Set(items.map((participant) => participant.group).filter(Boolean)).size,
    };
  },
  removeSession: mockRemoveSessionParticipants,
  reset: () => {
    participants = createParticipantFixtures();
  },
});

const sortAccessors: Record<ParticipantSortField, (participant: ParticipantDto) => string> = {
  [ParticipantSortField.LastName]: (participant) => `${participant.lastName ?? ''} ${participant.firstName ?? ''}`,
  [ParticipantSortField.FirstName]: (participant) => participant.firstName ?? '',
  [ParticipantSortField.Group]: (participant) => participant.group ?? '',
  [ParticipantSortField.School]: (participant) => participant.school ?? '',
  [ParticipantSortField.ExternalId]: (participant) => participant.externalId ?? '',
};

const matchesSearch = (participant: ParticipantDto, search: string) =>
  [participant.lastName, participant.firstName, participant.middleName, participant.externalId].some((value) => value?.toLowerCase().includes(search));

const queryPage = (sessionId: string, query: URLSearchParams): ParticipantDtoPagedDto => {
  const page = Number(query.get(ParticipantsQueryParam.Page) ?? FIRST_PAGE);
  const pageSize = Number(query.get(ParticipantsQueryParam.PageSize) ?? DEFAULT_PAGE_SIZE);
  const search = query.get(ParticipantsQueryParam.Search)?.trim().toLowerCase();
  const group = query.get(ParticipantsQueryParam.Group);
  const school = query.get(ParticipantsQueryParam.School)?.trim().toLowerCase();
  const accessible = query.get(ParticipantsQueryParam.NeedsAccessibleSeat);
  const sortBy = (query.get(ParticipantsQueryParam.SortBy) as ParticipantSortField | null) ?? ParticipantSortField.LastName;
  const direction = query.get(ParticipantsQueryParam.SortDirection) === SortDirection.Descending ? -1 : 1;

  const filtered = sessionParticipants(sessionId)
    .filter((participant) => !search || matchesSearch(participant, search))
    .filter((participant) => !group || participant.group === group)
    .filter((participant) => !school || participant.school?.toLowerCase().includes(school))
    .filter((participant) => accessible === null || participant.needsAccessibleSeat === (accessible === BOOLEAN_TRUE));
  const accessor = sortAccessors[sortBy] ?? sortAccessors[ParticipantSortField.LastName];
  const sorted = [...filtered].sort((left, right) => direction * accessor(left).localeCompare(accessor(right)));
  const start = (page - FIRST_PAGE) * pageSize;

  return { items: sorted.slice(start, start + pageSize), page, pageSize, total: filtered.length };
};

/** Built-in attributes summarized first, like the API (`ParticipantRepository` → `AttributeKeys.Group`, `.School`). */
export enum MockBuiltInAttributeKey {
  Group = 'group',
  School = 'school',
}

const builtInAttributeValue: Record<MockBuiltInAttributeKey, (participant: ParticipantDto) => string | null | undefined> = {
  [MockBuiltInAttributeKey.Group]: (participant) => participant.group,
  [MockBuiltInAttributeKey.School]: (participant) => participant.school,
};

const summarize = (key: string, items: ParticipantDto[], valueOf: (participant: ParticipantDto) => string | null | undefined): AttributeInfoDto => {
  const values = items.map(valueOf).filter((value): value is string => Boolean(value?.trim()));
  const distinct = [...new Set(values)];
  return { key, distinctValues: distinct.length, missingCount: items.length - values.length, sampleValues: distinct.slice(0, SAMPLE_VALUES_LIMIT) };
};

const computeAttributes = (sessionId: string): AttributeInfoDto[] => {
  const items = sessionParticipants(sessionId);
  const builtIn = Object.values(MockBuiltInAttributeKey).map((key) => summarize(key, items, builtInAttributeValue[key]));
  const keys = new Set(items.flatMap((participant) => Object.keys(participant.attributes ?? {})));
  const custom = [...keys].sort().map((key) => summarize(key, items, (participant) => participant.attributes?.[key]));
  return [...builtIn, ...custom];
};

const sessionIdParam = pathParam(MockPathParam.SessionId);
const participantPath = apiRoutes.participants.byId(sessionIdParam, pathParam(MockPathParam.ParticipantId));

export const participantsHandlers: HttpHandler[] = [
  http.get(mockUrl(apiRoutes.participants.list(sessionIdParam)), ({ params, request }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    if (!mockSessionExists(sessionId)) {
      return problem(HttpStatus.NotFound, ErrorCode.SessionNotFound);
    }
    return HttpResponse.json(queryPage(sessionId, new URL(request.url).searchParams));
  }),

  http.get(mockUrl(apiRoutes.participants.attributes(sessionIdParam)), ({ params }) => HttpResponse.json(computeAttributes(String(params[MockPathParam.SessionId])))),

  http.post(mockUrl(apiRoutes.participants.deleteAll(sessionIdParam)), ({ params }) => {
    mockRemoveSessionParticipants(String(params[MockPathParam.SessionId]));
    return noContent();
  }),

  http.post(mockUrl(apiRoutes.participants.create(sessionIdParam)), async ({ params, request }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    const dto = (await request.json()) as ParticipantDto;
    if (!dto.lastName?.trim() || !dto.firstName?.trim()) {
      return problem(HttpStatus.BadRequest, ErrorCode.ValidationFailed);
    }
    const created: ParticipantDto = { ...dto, id: newId(), sessionId, version: INITIAL_VERSION };
    participants = [...participants, created];
    touchMockSessionInput(sessionId);
    return HttpResponse.json(created);
  }),

  http.put(mockUrl(apiRoutes.participants.edit(sessionIdParam)), async ({ request }) => {
    const dto = (await request.json()) as ParticipantDto;
    const existing = participants.find((participant) => participant.id === dto.id);
    if (!existing) {
      return problem(HttpStatus.NotFound, ErrorCode.ParticipantNotFound);
    }
    if (existing.version !== dto.version) {
      return problem(HttpStatus.Conflict, ErrorCode.ConcurrencyConflict);
    }
    if (!dto.lastName?.trim() || !dto.firstName?.trim()) {
      return problem(HttpStatus.BadRequest, ErrorCode.ValidationFailed);
    }
    const updated: ParticipantDto = { ...existing, ...dto, version: (existing.version ?? INITIAL_VERSION) + 1 };
    participants = participants.map((participant) => (participant.id === dto.id ? updated : participant));
    touchMockSessionInput(existing.sessionId);
    return HttpResponse.json(updated);
  }),

  http.get(mockUrl(participantPath), ({ params }) => {
    const participant = participants.find((item) => item.id === params[MockPathParam.ParticipantId]);
    return participant ? HttpResponse.json(participant) : problem(HttpStatus.NotFound, ErrorCode.ParticipantNotFound);
  }),

  http.delete(mockUrl(participantPath), ({ params }) => {
    const participantId = String(params[MockPathParam.ParticipantId]);
    if (!participants.some((participant) => participant.id === participantId)) {
      return problem(HttpStatus.NotFound, ErrorCode.ParticipantNotFound);
    }
    touchMockSessionInput(participants.find((participant) => participant.id === participantId)?.sessionId);
    participants = participants.filter((participant) => participant.id !== participantId);
    return noContent();
  }),
];
