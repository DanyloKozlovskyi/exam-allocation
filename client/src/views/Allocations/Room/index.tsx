import { useNavigate, useParams } from 'react-router-dom';
import RoomSeating from '../../../components/Global/Allocations/Room';
import { routes } from '../../../constants/routes';

const AllocationsRoomView = () => {
  const { sessionId = '', allocationId = '', roomId = '' } = useParams();
  const navigate = useNavigate();
  return (
    <RoomSeating
      key={`${allocationId}-${roomId}`}
      sessionId={sessionId}
      allocationId={allocationId}
      roomId={roomId}
      onBack={() => void navigate(routes.allocations.view(sessionId, allocationId))}
      onOpenAllocationRoom={(id) => void navigate(routes.allocations.room(sessionId, id, roomId))}
      onRunStarted={(run) => run.id && void navigate(routes.solveRuns.view(sessionId, run.id))}
    />
  );
};

export default AllocationsRoomView;
