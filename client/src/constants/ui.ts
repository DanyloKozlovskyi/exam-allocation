import type { VariantType } from 'notistack';

export const LOCALE = 'uk-UA';
export const DRAWER_WIDTH_PX = 260;
export const SNACKBAR_MAX_STACK = 3;
export const SNACKBAR_AUTO_HIDE_MS = 3000;
export const FORM_GRID_SIZE = 8;

// notistack variants are a third-party literal type; named constants keep call sites free of magic strings.
export const SNACKBAR_SUCCESS: VariantType = 'success';
export const SNACKBAR_ERROR: VariantType = 'error';
export const SNACKBAR_INFO: VariantType = 'info';
