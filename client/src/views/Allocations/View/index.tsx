import { useNavigate, useParams } from 'react-router-dom';
import AllocationView from '../../../components/Global/Allocations/View';
import { routes } from '../../../constants/routes';

const AllocationsViewView = () => {
  const { sessionId = '', allocationId = '' } = useParams();
  const navigate = useNavigate();
  return (
    <AllocationView
      sessionId={sessionId}
      allocationId={allocationId}
      onSelectAllocation={(id) => void navigate(routes.allocations.view(sessionId, id))}
      onOpenRoom={(roomId) => void navigate(routes.allocations.room(sessionId, allocationId, roomId))}
      onRunStarted={(run) => run.id && void navigate(routes.solveRuns.view(sessionId, run.id))}
    />
  );
};

export default AllocationsViewView;
