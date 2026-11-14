import { useNavigate } from 'react-router-dom';
import SessionCreate from '../../../components/Global/Sessions/Create';
import { routes } from '../../../constants/routes';

const SessionsCreateView = () => {
  const navigate = useNavigate();
  return <SessionCreate onCreated={(session) => navigate(session.id ? routes.sessions.view(session.id) : routes.sessions.list())} />;
};

export default SessionsCreateView;
