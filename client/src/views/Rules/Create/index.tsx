import { useNavigate, useParams } from 'react-router-dom';
import RuleCreate from '../../../components/Global/Rules/Create';
import { routes } from '../../../constants/routes';

const RulesCreateView = () => {
  const { sessionId = '' } = useParams();
  const navigate = useNavigate();
  return <RuleCreate sessionId={sessionId} onFinish={() => void navigate(routes.rules.list(sessionId))} />;
};

export default RulesCreateView;
