import { useParams } from 'react-router-dom';
import PinsList from '../../../components/Global/Pins/List';

const PinsListView = () => {
  const { sessionId = '' } = useParams();
  return <PinsList sessionId={sessionId} />;
};

export default PinsListView;
