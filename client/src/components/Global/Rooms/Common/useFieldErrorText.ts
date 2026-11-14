import { useTranslation } from 'react-i18next';
import { I18nNamespace } from '../../../../i18n';
import type { FieldError } from '.';

export const useFieldErrorText = () => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  return (error: FieldError | undefined) => (error ? t(`validation.${error.error}`, { min: error.min, max: error.max }) : undefined);
};
