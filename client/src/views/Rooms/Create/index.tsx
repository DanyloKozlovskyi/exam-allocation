import { useNavigate, useParams } from 'react-router-dom';
import RoomCreate from '../../../components/Global/Rooms/Create';
import { routes } from '../../../constants/routes';

const RoomsCreateView = () => {
  const { sessionId = '', centerId = '' } = useParams();
  const navigate = useNavigate();
  return <RoomCreate centerId={centerId} onFinish={() => void navigate(routes.rooms.list(sessionId, centerId))} />;
};

export default RoomsCreateView;
