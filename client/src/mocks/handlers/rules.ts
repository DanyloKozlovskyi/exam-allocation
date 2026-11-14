/* eslint-disable @typescript-eslint/no-magic-numbers -- in-memory fixture data for MSW mocks */
import { http, HttpResponse, type HttpHandler } from 'msw';
import { AdjacencyKind, ErrorCode, RuleScope, RuleType, type AttributeInfoDto, type RuleDto, type RuleTypeInfoDto } from '../../api/dtos';
import {
  AccessibleSeatingParametersDtoType,
  FillFromFrontParametersDtoType,
  KeepTogetherInCenterParametersDtoType,
  MinRoomFillParametersDtoType,
  NoAdjacentByAttributeParametersDtoType,
  SeparateByAttributeParametersDtoType,
  SpreadInRoomParametersDtoType,
} from '../../api/generated/schema';
import { apiRoutes } from '../../constants/apiRoutes';
import { mockApiUrl, mockGuid, problem } from './rooms';

export const MOCK_RULE_TYPES: RuleTypeInfoDto[] = [
  {
    type: RuleType.SeparateByAttribute,
    allowsHard: true,
    allowsSoft: true,
    defaultParameters: { type: SeparateByAttributeParametersDtoType.SeparateByAttribute, attributeKey: 'school', scope: RuleScope.Room, maxPerScope: 1 },
  },
  {
    type: RuleType.NoAdjacentByAttribute,
    allowsHard: true,
    allowsSoft: true,
    defaultParameters: {
      type: NoAdjacentByAttributeParametersDtoType.NoAdjacentByAttribute,
      attributeKey: 'group',
      adjacency: [AdjacencyKind.SameDesk, AdjacencyKind.Side, AdjacencyKind.FrontBack],
      seatLevelSide: false,
    },
  },
  {
    type: RuleType.AccessibleSeating,
    allowsHard: true,
    allowsSoft: false,
    defaultParameters: { type: AccessibleSeatingParametersDtoType.AccessibleSeating } as RuleTypeInfoDto['defaultParameters'],
  },
  { type: RuleType.MinRoomFill, allowsHard: false, allowsSoft: true, defaultParameters: { type: MinRoomFillParametersDtoType.MinRoomFill, minFillRatio: 0.6 } },
  {
    type: RuleType.KeepTogetherInCenter,
    allowsHard: false,
    allowsSoft: true,
    defaultParameters: { type: KeepTogetherInCenterParametersDtoType.KeepTogetherInCenter, attributeKey: 'school' },
  },
  { type: RuleType.SpreadInRoom, allowsHard: false, allowsSoft: true, defaultParameters: { type: SpreadInRoomParametersDtoType.SpreadInRoom, adjacency: [AdjacencyKind.Side] } },
  {
    type: RuleType.FillFromFront,
    allowsHard: false,
    allowsSoft: true,
    defaultParameters: { type: FillFromFrontParametersDtoType.FillFromFront } as RuleTypeInfoDto['defaultParameters'],
  },
];

/** Attribute fixture for rule-form tests (the endpoint itself is served by the participants handlers). */
export const MOCK_ATTRIBUTES: AttributeInfoDto[] = [
  { key: 'group', distinctValues: 12, missingCount: 0, sampleValues: ['11-А', '11-Б', '10-В'] },
  { key: 'school', distinctValues: 6, missingCount: 3, sampleValues: ['Ліцей №142', 'Гімназія №1', 'ЗОШ №25'] },
  { key: 'language', distinctValues: 2, missingCount: 15, sampleValues: ['англійська', 'німецька'] },
];

const seedRules = (sessionId: string): RuleDto[] => [
  {
    id: mockGuid('b0'),
    sessionId,
    type: RuleType.NoAdjacentByAttribute,
    isHard: true,
    weight: 1,
    isEnabled: true,
    displayName: 'Однокласники не поруч',
    parameters: {
      type: NoAdjacentByAttributeParametersDtoType.NoAdjacentByAttribute,
      attributeKey: 'group',
      adjacency: [AdjacencyKind.SameDesk, AdjacencyKind.Side],
      seatLevelSide: false,
    },
    version: 1,
  },
  {
    id: mockGuid('b0'),
    sessionId,
    type: RuleType.SeparateByAttribute,
    isHard: false,
    weight: 50,
    isEnabled: true,
    displayName: null,
    parameters: { type: SeparateByAttributeParametersDtoType.SeparateByAttribute, attributeKey: 'school', scope: RuleScope.Room, maxPerScope: 4 },
    version: 1,
  },
  {
    id: mockGuid('b0'),
    sessionId,
    type: RuleType.FillFromFront,
    isHard: false,
    weight: 1,
    isEnabled: false,
    displayName: 'Заповнення з перших рядів',
    parameters: { type: FillFromFrontParametersDtoType.FillFromFront } as RuleDto['parameters'],
    version: 1,
  },
];

const rulesBySession = new Map<string, RuleDto[]>();

export const getMockRules = (sessionId: string) => {
  if (!rulesBySession.has(sessionId)) {
    rulesBySession.set(sessionId, seedRules(sessionId));
  }
  return rulesBySession.get(sessionId)!;
};

export const resetRulesMocks = () => rulesBySession.clear();

export const rulesHandlers: HttpHandler[] = [
  http.get(mockApiUrl(apiRoutes.rules.ruleTypes()), () => HttpResponse.json(MOCK_RULE_TYPES)),
  http.get(mockApiUrl(apiRoutes.rules.list(':sessionId')), ({ params }) => HttpResponse.json(getMockRules(String(params.sessionId)))),
  http.post(mockApiUrl(apiRoutes.rules.create(':sessionId')), async ({ params, request }) => {
    const sessionId = String(params.sessionId);
    const dto = (await request.json()) as RuleDto;
    const rule: RuleDto = { ...dto, id: mockGuid('b0'), sessionId, version: 1 };
    getMockRules(sessionId).push(rule);
    return HttpResponse.json(rule);
  }),
  http.put(mockApiUrl(apiRoutes.rules.edit(':sessionId')), async ({ params, request }) => {
    const rules = getMockRules(String(params.sessionId));
    const dto = (await request.json()) as RuleDto;
    const index = rules.findIndex((rule) => rule.id === dto.id);
    if (index < 0) {
      return problem(404, ErrorCode.RuleNotFound, 'Rule not found');
    }
    if (rules[index].version !== dto.version) {
      return problem(409, ErrorCode.ConcurrencyConflict, 'Concurrency conflict');
    }
    rules[index] = { ...dto, version: (dto.version ?? 0) + 1 };
    return HttpResponse.json(rules[index]);
  }),
  http.delete(mockApiUrl(apiRoutes.rules.byId(':sessionId', ':ruleId')), ({ params }) => {
    const rules = getMockRules(String(params.sessionId));
    const index = rules.findIndex((rule) => rule.id === params.ruleId);
    if (index < 0) {
      return problem(404, ErrorCode.RuleNotFound, 'Rule not found');
    }
    rules.splice(index, 1);
    return new HttpResponse(null, { status: 204 });
  }),
];
