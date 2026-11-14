import { Box } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { AdjacencyKind, RuleType } from '../../../../../api/dtos';
import { RulesTestId } from '../../../../../enums/Rules';
import { I18nNamespace } from '../../../../../i18n';
import ReadOnlyBlock, { type ReadOnlyItem } from '../../../../Common/ReadOnlyBlock';
import { PERCENT_FACTOR } from '../constants';
import { ADJACENCY_RULE_TYPES, ATTRIBUTE_RULE_TYPES } from '../parameters';
import { useAttributeLabel } from '../useAttributeLabel';
import type { RuleState } from '..';

interface Props {
  state: RuleState;
  setStep: (step: number) => void;
}

const StepReview = ({ state, setStep }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const { t: tRooms } = useTranslation(I18nNamespace.Rooms);
  const attributeLabel = useAttributeLabel();
  const { parameters } = state;

  const items: ReadOnlyItem[] = [
    { label: t('fields.type'), value: t(`ruleType.${state.type}`) },
    { label: t('fields.displayName'), value: state.displayName || t('list.noWeight') },
    { label: t('fields.kind'), value: state.isHard ? t('fields.hard') : t('fields.soft') },
  ];
  if (!state.isHard) items.push({ label: t('fields.weight'), value: state.weight });
  items.push({ label: t('fields.isEnabled'), value: state.isEnabled ? t('yes') : t('no') });
  if (ATTRIBUTE_RULE_TYPES.includes(state.type)) items.push({ label: t('fields.attributeKey'), value: attributeLabel(parameters.attributeKey) });
  if (state.type === RuleType.SeparateByAttribute) {
    items.push({ label: t('fields.scope'), value: t(`scope.${parameters.scope}`) }, { label: t('fields.maxPerScope'), value: parameters.maxPerScope });
  }
  if (ADJACENCY_RULE_TYPES.includes(state.type)) {
    items.push({ label: t('fields.adjacency'), value: parameters.adjacency.map((kind) => tRooms(`adjacencyKind.${kind}`)).join(', ') });
  }
  if (state.type === RuleType.NoAdjacentByAttribute && parameters.adjacency.includes(AdjacencyKind.Side)) {
    items.push({ label: t('fields.seatLevelSide'), value: parameters.seatLevelSide ? t('yes') : t('no') });
  }
  if (state.type === RuleType.MinRoomFill) items.push({ label: t('fields.minFillRatio'), value: Math.round(parameters.minFillRatio * PERCENT_FACTOR) });

  return (
    <Box data-qa={RulesTestId.Summary}>
      <ReadOnlyBlock items={items} handleEdit={() => setStep(0)} />
    </Box>
  );
};

export default StepReview;
