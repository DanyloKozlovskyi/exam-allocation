import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const RulesEditView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.ruleEdit')} />;
};

export default RulesEditView;
