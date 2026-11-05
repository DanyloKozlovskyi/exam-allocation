import { createTheme } from '@mui/material/styles';

const PRIMARY_MAIN = '#1976D2';
const SECONDARY_MAIN = '#FFCE00';
const BACKGROUND_DEFAULT = '#F4F4F4';

export const theme = createTheme({
  palette: {
    primary: { main: PRIMARY_MAIN },
    secondary: { main: SECONDARY_MAIN },
    background: { default: BACKGROUND_DEFAULT },
  },
});
