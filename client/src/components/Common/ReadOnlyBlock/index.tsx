import { Box, Button, Grid, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';

export interface ReadOnlyItem {
  label: string;
  value: React.ReactNode;
}

interface Props {
  items: ReadOnlyItem[];
  handleEdit?: () => void;
}

const LABEL_COLUMNS = 4;
const VALUE_COLUMNS = 8;

const ReadOnlyBlock = ({ items, handleEdit }: Props) => {
  const { t } = useTranslation();
  return (
    <Box>
      <Grid container spacing={1}>
        {items.map((item) => (
          <Grid container key={item.label} size={12}>
            <Grid size={LABEL_COLUMNS}>
              <Typography color="text.secondary">{item.label}</Typography>
            </Grid>
            <Grid size={VALUE_COLUMNS}>
              <Typography>{item.value}</Typography>
            </Grid>
          </Grid>
        ))}
      </Grid>
      {handleEdit && (
        <Button sx={{ mt: 2 }} onClick={handleEdit}>
          {t('actions.edit')}
        </Button>
      )}
    </Box>
  );
};

export default ReadOnlyBlock;
