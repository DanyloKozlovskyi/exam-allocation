import { AdjacencyKind, RuleScope, RuleType, type RuleDto, type RuleTypeInfoDto } from '../../../../api/dtos';
import {
  AccessibleSeatingParametersDtoType,
  FillFromFrontParametersDtoType,
  KeepTogetherInCenterParametersDtoType,
  MinRoomFillParametersDtoType,
  NoAdjacentByAttributeParametersDtoType,
  SeparateByAttributeParametersDtoType,
  SpreadInRoomParametersDtoType,
} from '../../../../api/dtos';
import { BuiltInAttributeKey } from '../../../../enums/Rules';
import { RULE_DEFAULT_MIN_FILL_RATIO, RULE_MIN_PER_SCOPE } from './constants';

export type RuleParametersDtoUnion = NonNullable<RuleDto['parameters']>;

/** Flat editable state covering every parameter shape; only the fields of the selected `RuleType` are sent. */
export interface RuleParametersState {
  attributeKey: string;
  scope: RuleScope;
  maxPerScope: number;
  adjacency: AdjacencyKind[];
  seatLevelSide: boolean;
  minFillRatio: number;
}

export const defaultParametersState: RuleParametersState = {
  attributeKey: BuiltInAttributeKey.Group,
  scope: RuleScope.Room,
  maxPerScope: RULE_MIN_PER_SCOPE,
  adjacency: [AdjacencyKind.SameDesk, AdjacencyKind.Side],
  seatLevelSide: false,
  minFillRatio: RULE_DEFAULT_MIN_FILL_RATIO,
};

export const fromParametersDto = (dto: RuleParametersDtoUnion | undefined, base: RuleParametersState = defaultParametersState): RuleParametersState => {
  if (!dto) {
    return base;
  }

  const source = dto as Partial<RuleParametersState>;
  return {
    attributeKey: source.attributeKey ?? base.attributeKey,
    scope: source.scope ?? base.scope,
    maxPerScope: source.maxPerScope ?? base.maxPerScope,
    adjacency: source.adjacency ?? base.adjacency,
    seatLevelSide: source.seatLevelSide ?? base.seatLevelSide,
    minFillRatio: source.minFillRatio ?? base.minFillRatio,
  };
};

const PARAMETER_BUILDERS: Record<RuleType, (state: RuleParametersState) => RuleParametersDtoUnion> = {
  [RuleType.SeparateByAttribute]: (state) => ({
    type: SeparateByAttributeParametersDtoType.SeparateByAttribute,
    attributeKey: state.attributeKey,
    scope: state.scope,
    maxPerScope: state.maxPerScope,
  }),
  [RuleType.NoAdjacentByAttribute]: (state) => ({
    type: NoAdjacentByAttributeParametersDtoType.NoAdjacentByAttribute,
    attributeKey: state.attributeKey,
    adjacency: state.adjacency,
    seatLevelSide: state.adjacency.includes(AdjacencyKind.Side) && state.seatLevelSide,
  }),
  [RuleType.AccessibleSeating]: () => ({ type: AccessibleSeatingParametersDtoType.AccessibleSeating }) as RuleParametersDtoUnion,
  [RuleType.MinRoomFill]: (state) => ({ type: MinRoomFillParametersDtoType.MinRoomFill, minFillRatio: state.minFillRatio }),
  [RuleType.KeepTogetherInCenter]: (state) => ({ type: KeepTogetherInCenterParametersDtoType.KeepTogetherInCenter, attributeKey: state.attributeKey }),
  [RuleType.SpreadInRoom]: (state) => ({ type: SpreadInRoomParametersDtoType.SpreadInRoom, adjacency: state.adjacency }),
  [RuleType.FillFromFront]: () => ({ type: FillFromFrontParametersDtoType.FillFromFront }) as RuleParametersDtoUnion,
};

export const toParametersDto = (type: RuleType, state: RuleParametersState) => PARAMETER_BUILDERS[type](state);

/** Rule types whose parameters reference a participant attribute. */
export const ATTRIBUTE_RULE_TYPES: RuleType[] = [RuleType.SeparateByAttribute, RuleType.NoAdjacentByAttribute, RuleType.KeepTogetherInCenter];

/** Rule types whose parameters include adjacency kinds. */
export const ADJACENCY_RULE_TYPES: RuleType[] = [RuleType.NoAdjacentByAttribute, RuleType.SpreadInRoom];

export const findTypeInfo = (types: RuleTypeInfoDto[] | undefined, type: RuleType) => types?.find((info) => info.type === type);
