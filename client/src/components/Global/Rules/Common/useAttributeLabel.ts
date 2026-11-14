import { useTranslation } from 'react-i18next';
import { BuiltInAttributeKey } from '../../../../enums/Rules';
import { I18nNamespace } from '../../../../i18n';

const BUILT_IN_KEYS = new Set<string>(Object.values(BuiltInAttributeKey));

/** Ukrainian label for built-in attribute keys; custom keys are shown as-is. */
export const useAttributeLabel = () => {
  const { t } = useTranslation(I18nNamespace.Rules);
  return (key: string) => (BUILT_IN_KEYS.has(key) ? t(`attribute.${key as BuiltInAttributeKey}`) : key);
};
