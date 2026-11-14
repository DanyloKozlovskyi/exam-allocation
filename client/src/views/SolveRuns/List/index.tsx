import { useNavigate, useParams } from 'react-router-dom';
import SolveRunsList from '../../../components/Global/SolveRuns/List';
import { routes } from '../../../constants/routes';

const SolveRunsListView = () => {
  const { sessionId = '' } = useParams();
  const navigate = useNavigate();
  return <SolveRunsList sessionId={sessionId} onOpen={(runId) => void navigate(routes.solveRuns.view(sessionId, runId))} />;
};

export default SolveRunsListView;
