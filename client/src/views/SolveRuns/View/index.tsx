import { useParams } from 'react-router-dom';
import SolveRunView from '../../../components/Global/SolveRuns/View';

const SolveRunsViewView = () => {
  const { sessionId = '', runId = '' } = useParams();
  return <SolveRunView sessionId={sessionId} runId={runId} />;
};

export default SolveRunsViewView;
