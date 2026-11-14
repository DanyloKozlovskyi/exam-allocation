import { useNavigate, useParams } from 'react-router-dom';
import RulesList from '../../../components/Global/Rules/List';
import { routes } from '../../../constants/routes';

const RulesListView = () => {
  const { sessionId = '' } = useParams();
  const navigate = useNavigate();
  return (
    <RulesList sessionId={sessionId} onCreate={() => void navigate(routes.rules.create(sessionId))} onEdit={(ruleId) => void navigate(routes.rules.edit(sessionId, ruleId))} />
  );
};

export default RulesListView;
