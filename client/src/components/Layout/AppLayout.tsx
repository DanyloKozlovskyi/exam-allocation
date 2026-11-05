import { AppBar, Box, Drawer, List, ListItemButton, ListItemText, Toolbar, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink, Outlet, useParams } from 'react-router-dom';
import { routes } from '../../constants/routes';
import { LayoutTestId } from '../../constants/testIds';
import { DRAWER_WIDTH_PX } from '../../constants/ui';

interface NavigationItem {
  label: string;
  to: string;
}

const AppLayout = () => {
  const { t } = useTranslation();
  const { sessionId } = useParams();

  const items: NavigationItem[] = [{ label: t('navigation.sessions'), to: routes.sessions.list() }];
  if (sessionId) {
    items.push(
      { label: t('navigation.overview'), to: routes.sessions.view(sessionId) },
      { label: t('navigation.centers'), to: routes.centers.list(sessionId) },
      { label: t('navigation.participants'), to: routes.participants.list(sessionId) },
      { label: t('navigation.rules'), to: routes.rules.list(sessionId) },
      { label: t('navigation.solveRuns'), to: routes.solveRuns.list(sessionId) }
    );
  }

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar position="fixed" sx={{ zIndex: (muiTheme) => muiTheme.zIndex.drawer + 1 }}>
        <Toolbar>
          <Typography variant="h6" noWrap data-qa={LayoutTestId.AppTitle}>
            {t('appTitle')}
          </Typography>
        </Toolbar>
      </AppBar>
      <Drawer variant="permanent" sx={{ width: DRAWER_WIDTH_PX, flexShrink: 0, '& .MuiDrawer-paper': { width: DRAWER_WIDTH_PX, boxSizing: 'border-box' } }}>
        <Toolbar />
        <List data-qa={LayoutTestId.Navigation}>
          {items.map((item) => (
            <ListItemButton key={item.to} component={RouterLink} to={item.to}>
              <ListItemText primary={item.label} />
            </ListItemButton>
          ))}
        </List>
      </Drawer>
      <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
};

export default AppLayout;
