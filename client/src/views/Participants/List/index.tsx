import { useParams } from 'react-router-dom';
import ParticipantsList from '../../../components/Global/Participants/List';

const ParticipantsListView = () => {
  const { sessionId = '' } = useParams();
  return <ParticipantsList sessionId={sessionId} />;
};

export default ParticipantsListView;
