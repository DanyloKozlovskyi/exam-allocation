import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const CentersCreateView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.centerCreate')} />;
};

export default CentersCreateView;
