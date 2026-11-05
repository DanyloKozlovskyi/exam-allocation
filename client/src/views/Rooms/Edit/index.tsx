import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const RoomsEditView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.roomEdit')} />;
};

export default RoomsEditView;
