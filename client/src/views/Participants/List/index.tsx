import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const ParticipantsListView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.participantsList')} />;
};

export default ParticipantsListView;
