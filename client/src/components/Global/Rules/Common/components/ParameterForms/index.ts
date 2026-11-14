import type { FC } from 'react';
import { RuleType } from '../../../../../../api/dtos';
import AdjacencyCheckboxes from './AdjacencyCheckboxes';
import AttributeSelect from './AttributeSelect';
import { MinRoomFillForm, NoAdjacentByAttributeForm, NoParametersForm, SeparateByAttributeForm } from './Forms';
import type { ParametersFormProps } from './types';

export type { ParametersFormProps } from './types';

/** Per-`RuleType` parameters form (docs/06 Rules). */
export const PARAMETER_FORMS: Record<RuleType, FC<ParametersFormProps>> = {
  [RuleType.SeparateByAttribute]: SeparateByAttributeForm,
  [RuleType.NoAdjacentByAttribute]: NoAdjacentByAttributeForm,
  [RuleType.AccessibleSeating]: NoParametersForm,
  [RuleType.MinRoomFill]: MinRoomFillForm,
  [RuleType.KeepTogetherInCenter]: AttributeSelect,
  [RuleType.SpreadInRoom]: AdjacencyCheckboxes,
  [RuleType.FillFromFront]: NoParametersForm,
};
