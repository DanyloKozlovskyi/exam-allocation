import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const SessionsCreateView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.sessionCreate')} />;
};

export default SessionsCreateView;
