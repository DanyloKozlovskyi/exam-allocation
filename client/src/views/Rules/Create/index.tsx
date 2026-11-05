import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const RulesCreateView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.ruleCreate')} />;
};

export default RulesCreateView;
