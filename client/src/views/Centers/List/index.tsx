import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const CentersListView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.centersList')} />;
};

export default CentersListView;
