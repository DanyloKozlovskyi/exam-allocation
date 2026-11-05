import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const RoomsCreateView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.roomCreate')} />;
};

export default RoomsCreateView;
