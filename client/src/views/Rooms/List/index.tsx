import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const RoomsListView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.roomsList')} />;
};

export default RoomsListView;
