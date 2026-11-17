import { useLocation, useParams } from 'react-router-dom';
import ParticipantsList from '../../../components/Global/Participants/List';

const ParticipantsListView = () => {
  const { sessionId = '' } = useParams();
  const { search } = useLocation();
  // Filters are preset from the URL; remount when the query changes (drawer link, deep link, back/forward).
  return <ParticipantsList key={search} sessionId={sessionId} />;
};

export default ParticipantsListView;
