import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const SolveRunsViewView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.solveRunView')} />;
};

export default SolveRunsViewView;
