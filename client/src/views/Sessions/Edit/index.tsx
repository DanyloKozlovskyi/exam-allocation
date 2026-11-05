import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const SessionsEditView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.sessionEdit')} />;
};

export default SessionsEditView;
