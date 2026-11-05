import { useTranslation } from 'react-i18next';
import PlaceholderView from '../../../components/Common/PlaceholderView';

const SolveRunsListView = () => {
  const { t } = useTranslation();
  return <PlaceholderView title={t('views.solveRunsList')} />;
};

export default SolveRunsListView;
