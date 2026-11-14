import { useParams } from 'react-router-dom';
import SessionView from '../../../components/Global/Sessions/View';

const SessionsViewView = () => {
  const { sessionId = '' } = useParams();
  return <SessionView sessionId={sessionId} />;
};

export default SessionsViewView;
