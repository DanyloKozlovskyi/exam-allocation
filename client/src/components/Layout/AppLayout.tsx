import { AppBar, Box, Chip, Drawer, List, ListItemButton, ListItemText, Toolbar, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink, Outlet, useParams } from 'react-router-dom';
import { LayoutTestId } from '../../constants/testIds';
import { DRAWER_WIDTH_PX } from '../../constants/ui';
import { useSessionTotalsQuery } from '../Global/Queries/sessions';
import { useSolveRunsListQuery } from '../Global/Queries/solveRuns';
import { buildNavigation } from './navigation';

const AppLayout = () => {
  const { t } = useTranslation();
  const { sessionId = '', centerId } = useParams();
  const { data: totals } = useSessionTotalsQuery(sessionId);
  const { data: runs } = useSolveRunsListQuery(sessionId);
  const items = buildNavigation({ sessionId: sessionId || undefined, centerId, totals, runs });

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
            <ListItemButton key={item.key} component={RouterLink} to={item.to} data-qa={`${LayoutTestId.NavigationItem}-${item.key}`}>
              <ListItemText primary={t(`navigation.${item.key}`)} />
              {item.badge !== undefined && <Chip size="small" label={item.badge} data-qa={`${LayoutTestId.NavigationBadge}-${item.key}`} />}
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
