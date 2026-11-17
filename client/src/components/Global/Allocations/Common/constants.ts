import { ExportFormat } from '../../../../api/dtos';

/** Stable group colours (hash of group value → palette index). */
export const GROUP_PALETTE = ['#E57373', '#64B5F6', '#81C784', '#FFB74D', '#BA68C8', '#4DB6AC', '#F06292', '#A1887F', '#90A4AE', '#DCE775', '#7986CB', '#FFD54F'];
export const NO_GROUP_COLOR = '#CFD8DC';

const HASH_SEED = 2166136261;
const HASH_PRIME = 16777619;

/** FNV-1a hash: the same group always gets the same colour across rooms and sessions. */
export const groupColor = (group: string | null | undefined) => {
  if (!group) {
    return NO_GROUP_COLOR;
  }
  let hash = HASH_SEED;
  for (const char of group) {
    hash = Math.imul(hash ^ (char.codePointAt(0) ?? 0), HASH_PRIME) >>> 0;
  }
  return GROUP_PALETTE[hash % GROUP_PALETTE.length];
};

export const EXPORT_FORMATS: ExportFormat[] = [ExportFormat.Xlsx, ExportFormat.Xml, ExportFormat.Csv, ExportFormat.Pdf];

/** Index of the hand-edited allocation (`apply-moves`). */
export const MANUAL_ALLOCATION_INDEX = -1;

export const PERCENT_FACTOR = 100;
export const FULL_RATIO = 1;
export const SEATING_SEAT_WIDTH_PX = 76;
export const SURNAME_MAX_CHARS = 9;
export const ELLIPSIS = '…';
export const GROUP_BAR_HEIGHT_PX = 6;
export const SEAT_TEXT_OFFSET_PX = 4;
export const SEAT_TEXT_FONT_SIZE_PX = 11;

/** Fill-ratio colouring of overview markers: [min ratio, colour], checked from the top. */
const FILL_RATIO_HIGH = 0.9;
const FILL_RATIO_GOOD = 0.6;
const FILL_RATIO_LOW = 0.3;
export const FILL_RATIO_COLORS: [number, string][] = [
  [FILL_RATIO_HIGH, '#2E7D32'],
  [FILL_RATIO_GOOD, '#66BB6A'],
  [FILL_RATIO_LOW, '#FFA726'],
  [0, '#EF5350'],
];

export const fillRatioColor = (ratio: number) => FILL_RATIO_COLORS.find(([min]) => ratio >= min)?.[1] ?? NO_GROUP_COLOR;
