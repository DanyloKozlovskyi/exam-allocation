import { Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { LayoutTestId } from '../../../constants/testIds';

interface Props {
  title: string;
}

/** Phase 0 placeholder rendered by every view until its workstream implements it. */
const PlaceholderView = ({ title }: Props) => {
  const { t } = useTranslation();
  return (
    <div data-qa={LayoutTestId.PlaceholderView}>
      <Typography variant="h5" gutterBottom>
        {title}
      </Typography>
      <Typography color="text.secondary">{t('placeholder')}</Typography>
    </div>
  );
};

export default PlaceholderView;
