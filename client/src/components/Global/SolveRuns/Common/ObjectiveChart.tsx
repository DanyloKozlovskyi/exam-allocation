import { Box, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { SolveRunsTestId } from '../../../../enums/SolveRuns';
import { I18nNamespace } from '../../../../i18n';
import type { ObjectivePoint } from '../../../../hooks/useSolveProgress';

interface Props {
  history: ObjectivePoint[];
}

const WIDTH_PX = 320;
const HEIGHT_PX = 96;
const PADDING_PX = 6;
const STROKE_WIDTH_PX = 2;
const OBJECTIVE_COLOR = '#1976D2';
const BOUND_COLOR = '#9E9E9E';
const BOUND_DASH = '4 3';
const MIN_POINTS = 2;

/** Mini line chart of objective (solid) and best bound (dashed) reported by the hub. */
const ObjectiveChart = ({ history }: Props) => {
  const { t } = useTranslation(I18nNamespace.SolveRuns);
  if (history.length < MIN_POINTS) {
    return null;
  }

  const values = history.flatMap((point) => (point.bestBound === null ? [point.objective] : [point.objective, point.bestBound]));
  const min = Math.min(...values);
  const max = Math.max(...values);
  const span = max - min || 1;
  const x = (index: number) => PADDING_PX + (index / (history.length - 1)) * (WIDTH_PX - PADDING_PX * MIN_POINTS);
  const y = (value: number) => HEIGHT_PX - PADDING_PX - ((value - min) / span) * (HEIGHT_PX - PADDING_PX * MIN_POINTS);
  const line = (pick: (point: ObjectivePoint) => number | null) =>
    history.flatMap((point, index) => {
      const value = pick(point);
      return value === null ? [] : [`${x(index)},${y(value)}`];
    });
  const bound = line((point) => point.bestBound);

  return (
    <Box data-qa={SolveRunsTestId.ObjectiveChart}>
      <Typography variant="caption" color="text.secondary">
        {t('view.chartTitle')}
      </Typography>
      <svg width={WIDTH_PX} height={HEIGHT_PX} role="img" aria-label={t('view.chartTitle')}>
        <polyline fill="none" stroke={OBJECTIVE_COLOR} strokeWidth={STROKE_WIDTH_PX} points={line((point) => point.objective).join(' ')} />
        {bound.length >= MIN_POINTS && <polyline fill="none" stroke={BOUND_COLOR} strokeDasharray={BOUND_DASH} strokeWidth={STROKE_WIDTH_PX} points={bound.join(' ')} />}
      </svg>
    </Box>
  );
};

export default ObjectiveChart;
