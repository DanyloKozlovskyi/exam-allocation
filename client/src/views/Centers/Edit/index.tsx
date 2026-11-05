import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const CentersEditView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.centerEdit')} />;
};

export default CentersEditView;
