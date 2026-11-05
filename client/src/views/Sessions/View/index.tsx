import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const SessionsViewView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.sessionView')} />;
};

export default SessionsViewView;
