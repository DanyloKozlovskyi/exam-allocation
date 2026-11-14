import { useNavigate, useParams } from 'react-router-dom';
import ImportWizard from '../../../components/Global/Imports/Create';
import { routes } from '../../../constants/routes';

const ParticipantsImportView = () => {
  const { sessionId = '' } = useParams();
  const navigate = useNavigate();
  return <ImportWizard sessionId={sessionId} onFinished={() => navigate(routes.participants.list(sessionId))} />;
};

export default ParticipantsImportView;
