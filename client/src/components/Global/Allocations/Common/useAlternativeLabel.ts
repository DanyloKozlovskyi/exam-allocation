import { useTranslation } from 'react-i18next';
import { I18nNamespace } from '../../../../i18n';
import { MANUAL_ALLOCATION_INDEX } from './constants';

export const useAlternativeLabel = () => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  return (index: number | undefined) => {
    if (index === MANUAL_ALLOCATION_INDEX) return t('alternative.manual');
    return index ? t('alternative.numbered', { index }) : t('alternative.best');
  };
};
