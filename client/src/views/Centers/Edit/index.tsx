import { useNavigate, useParams } from 'react-router-dom';
import CenterEdit from '../../../components/Global/Centers/Edit';
import { routes } from '../../../constants/routes';

const CentersEditView = () => {
  const { sessionId = '', centerId = '' } = useParams();
  const navigate = useNavigate();
  return <CenterEdit sessionId={sessionId} centerId={centerId} onSaved={() => navigate(routes.centers.list(sessionId))} />;
};

export default CentersEditView;
