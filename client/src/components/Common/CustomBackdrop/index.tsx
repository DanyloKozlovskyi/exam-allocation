import { Backdrop, CircularProgress, Stack, Typography } from '@mui/material';

interface Props {
  label: string;
}

const CustomBackdrop = ({ label }: Props) => (
  <Backdrop open sx={{ color: 'common.white', zIndex: (muiTheme) => muiTheme.zIndex.modal + 1 }}>
    <Stack spacing={2} sx={{ alignItems: 'center' }}>
      <CircularProgress color="inherit" />
      <Typography>{label}</Typography>
    </Stack>
  </Backdrop>
);

export default CustomBackdrop;
