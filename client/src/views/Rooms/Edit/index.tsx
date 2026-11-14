import { useNavigate, useParams } from 'react-router-dom';
import RoomEdit from '../../../components/Global/Rooms/Edit';
import { routes } from '../../../constants/routes';

const RoomsEditView = () => {
  const { sessionId = '', centerId = '', roomId = '' } = useParams();
  const navigate = useNavigate();
  return <RoomEdit centerId={centerId} roomId={roomId} onFinish={() => void navigate(routes.rooms.list(sessionId, centerId))} />;
};

export default RoomsEditView;
