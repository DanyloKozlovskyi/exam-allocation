import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const RulesListView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.rulesList')} />;
};

export default RulesListView;
