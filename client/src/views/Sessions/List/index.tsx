import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const SessionsListView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.sessionsList')} />;
};

export default SessionsListView;
