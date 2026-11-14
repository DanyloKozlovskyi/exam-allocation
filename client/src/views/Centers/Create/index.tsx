import { useLocation, useNavigate, useParams } from 'react-router-dom';
import CenterCreate from '../../../components/Global/Centers/Create';
import type { PickedPlace } from '../../../components/Global/Centers/Map/types';
import { routes } from '../../../constants/routes';

const CentersCreateView = () => {
  const { sessionId = '' } = useParams();
  const navigate = useNavigate();
  const { state } = useLocation();
  return <CenterCreate sessionId={sessionId} prefill={state as Partial<PickedPlace> | null} onCreated={() => navigate(routes.centers.list(sessionId))} />;
};

export default CentersCreateView;
