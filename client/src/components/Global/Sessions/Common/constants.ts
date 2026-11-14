import type { GridFeatureMode } from '@mui/x-data-grid';

// UI sizes shared by the WS-E features (constants/ui.ts is shared — kept local per docs/09 owned paths).
export const FORM_SKELETON_HEIGHT_PX = 240;
export const GRID_HEIGHT_PX = 560;
export const GRID_SKELETON_ROWS = 5;
export const CARD_GRID_SIZE_SM = 6;
export const CARD_GRID_SIZE_MD = 3;
export const HALF_GRID_SIZE = 6;
export const FULL_GRID_SIZE = 12;

// MUI X literal types; named constants keep call sites free of magic strings.
export const GRID_SERVER_MODE: GridFeatureMode = 'server';
