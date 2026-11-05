import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const AllocationsRoomView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.allocationRoom')} />;
};

export default AllocationsRoomView;
