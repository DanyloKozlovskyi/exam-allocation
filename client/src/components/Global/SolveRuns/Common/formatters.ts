import { LOCALE } from '../../../../constants/ui';
import { MS_PER_SECOND } from '../../../../constants/queryParams';

const dateTimeFormat = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'short', timeStyle: 'short' });
const numberFormat = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 2 });

export const formatDateTime = (iso: string | null | undefined) => (iso ? dateTimeFormat.format(new Date(iso)) : '');

export const formatNumber = (value: number | null | undefined) => (value === null || value === undefined ? '' : numberFormat.format(value));

/** Run duration in seconds, or null while it has not finished. */
export const durationSeconds = (startedAt: string | null | undefined, finishedAt: string | null | undefined) =>
  startedAt && finishedAt ? Math.max(0, Math.round((Date.parse(finishedAt) - Date.parse(startedAt)) / MS_PER_SECOND)) : null;
