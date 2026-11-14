import { LOCALE } from '../../../../constants/ui';

const dateFormatter = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium' });
const dateTimeFormatter = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium', timeStyle: 'short' });
const numberFormatter = new Intl.NumberFormat(LOCALE);

export const formatDate = (value?: string | null) => (value ? dateFormatter.format(new Date(value)) : '');
export const formatDateTime = (value?: string | null) => (value ? dateTimeFormatter.format(new Date(value)) : '');
export const formatNumber = (value?: number | null) => numberFormatter.format(value ?? 0);
