import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const AllocationsViewView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.allocationView')} />;
};

export default AllocationsViewView;
