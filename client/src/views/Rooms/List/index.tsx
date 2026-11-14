import { useNavigate, useParams } from 'react-router-dom';
import RoomsList from '../../../components/Global/Rooms/List';
import { routes } from '../../../constants/routes';

const RoomsListView = () => {
  const { sessionId = '', centerId = '' } = useParams();
  const navigate = useNavigate();
  return (
    <RoomsList
      centerId={centerId}
      onCreate={() => void navigate(routes.rooms.create(sessionId, centerId))}
      onEdit={(roomId) => void navigate(routes.rooms.edit(sessionId, centerId, roomId))}
    />
  );
};

export default RoomsListView;
