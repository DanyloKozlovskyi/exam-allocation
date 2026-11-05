import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const ParticipantsImportView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.participantsImport')} />;
};

export default ParticipantsImportView;
