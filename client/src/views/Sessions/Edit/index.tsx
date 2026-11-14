import { useNavigate, useParams } from 'react-router-dom';
import SessionEdit from '../../../components/Global/Sessions/Edit';
import { routes } from '../../../constants/routes';

const SessionsEditView = () => {
  const { sessionId = '' } = useParams();
  const navigate = useNavigate();
  return <SessionEdit sessionId={sessionId} onSaved={() => navigate(routes.sessions.view(sessionId))} />;
};

export default SessionsEditView;
