import { useNavigate, useParams } from 'react-router-dom';
import RuleEdit from '../../../components/Global/Rules/Edit';
import { routes } from '../../../constants/routes';

const RulesEditView = () => {
  const { sessionId = '', ruleId = '' } = useParams();
  const navigate = useNavigate();
  return <RuleEdit sessionId={sessionId} ruleId={ruleId} onFinish={() => void navigate(routes.rules.list(sessionId))} />;
};

export default RulesEditView;
