import { CssBaseline, ThemeProvider } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { QueryClientProvider } from '@tanstack/react-query';
import { SnackbarProvider } from 'notistack';
import { BrowserRouter } from 'react-router-dom';
import AppRoutes from '../../Routes';
import { SNACKBAR_AUTO_HIDE_MS, SNACKBAR_MAX_STACK } from '../../constants/ui';
import { Language } from '../../i18n';
import { createQueryClient } from './queryClient';
import { theme } from './theme';

const queryClient = createQueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <ThemeProvider theme={theme}>
      <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale={Language.Ukrainian}>
        <SnackbarProvider maxSnack={SNACKBAR_MAX_STACK} autoHideDuration={SNACKBAR_AUTO_HIDE_MS} anchorOrigin={{ vertical: 'top', horizontal: 'right' }}>
          <CssBaseline />
          <BrowserRouter>
            <AppRoutes />
          </BrowserRouter>
        </SnackbarProvider>
      </LocalizationProvider>
    </ThemeProvider>
  </QueryClientProvider>
);

export default App;
