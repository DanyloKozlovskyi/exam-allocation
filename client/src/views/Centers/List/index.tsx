import { useNavigate, useParams } from 'react-router-dom';
import CentersList from '../../../components/Global/Centers/List';
import { routes } from '../../../constants/routes';

const CentersListView = () => {
  const { sessionId = '' } = useParams();
  const navigate = useNavigate();
  // The prefill travels as router state so the Create route needs no extra query parameters.
  return <CentersList sessionId={sessionId} onCreate={(prefill) => navigate(routes.centers.create(sessionId), { state: prefill ?? null })} />;
};

export default CentersListView;
