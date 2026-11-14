import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import EditIcon from '@mui/icons-material/Edit';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutlineOutlined';
import { Alert, Button, Card, CardContent, Grid, List, ListItem, ListItemIcon, ListItemText, Paper, Skeleton, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import { routes } from '../../../../constants/routes';
import { SessionTotal, SessionsTestId } from '../../../../enums/Sessions';
import { I18nNamespace } from '../../../../i18n';
import ReadOnlyBlock from '../../../Common/ReadOnlyBlock';
import { useSessionQuery, useSessionTotalsQuery } from '../../Queries/sessions';
import { CARD_GRID_SIZE_MD, CARD_GRID_SIZE_SM, FORM_SKELETON_HEIGHT_PX, FULL_GRID_SIZE } from '../Common/constants';
import { buildChecklist } from '../Common/checklist';
import { formatDate, formatNumber } from '../Common/formatters';

interface Props {
  sessionId: string;
}

const SessionView = ({ sessionId }: Props) => {
  const { t } = useTranslation(I18nNamespace.Sessions);
  const session = useSessionQuery(sessionId);
  const totals = useSessionTotalsQuery(sessionId);

  if (session.isError) {
    return <Alert severity="error">{t('view.notFound')}</Alert>;
  }

  return (
    <Stack spacing={3} data-qa={SessionsTestId.Dashboard}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5">{session.data?.name ?? <Skeleton width={FORM_SKELETON_HEIGHT_PX} />}</Typography>
        <Button startIcon={<EditIcon />} component={RouterLink} to={routes.sessions.edit(sessionId)} data-qa={SessionsTestId.EditButton}>
          {t('view.edit')}
        </Button>
      </Stack>

      {totals.data?.isStale && (
        <Alert severity="warning" data-qa={SessionsTestId.StaleWarning}>
          {t('view.stale')}
        </Alert>
      )}

      {session.data && (
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6" gutterBottom>
            {t('view.detailsTitle')}
          </Typography>
          <ReadOnlyBlock
            items={[
              { label: t('form.fields.examDate'), value: session.data.examDate ? formatDate(session.data.examDate) : t('form.notSet') },
              { label: t('form.fields.description'), value: session.data.description || t('form.notSet') },
            ]}
          />
        </Paper>
      )}

      <Stack spacing={1}>
        <Typography variant="h6">{t('view.totalsTitle')}</Typography>
        <Grid container spacing={2} data-qa={SessionsTestId.Totals}>
          {Object.values(SessionTotal).map((key) => (
            <Grid key={key} size={{ xs: FULL_GRID_SIZE, sm: CARD_GRID_SIZE_SM, md: CARD_GRID_SIZE_MD }}>
              <Card variant="outlined">
                <CardContent>
                  <Typography color="text.secondary" variant="body2">
                    {t(`view.totals.${key}`)}
                  </Typography>
                  <Typography variant="h5" data-qa={`${SessionsTestId.Totals}-${key}`}>
                    {totals.data ? formatNumber(totals.data[key]) : <Skeleton />}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </Stack>

      {totals.data && (
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6">{t('view.checklistTitle')}</Typography>
          <List data-qa={SessionsTestId.Checklist}>
            {buildChecklist(sessionId, totals.data).map((entry) => (
              <ListItem
                key={entry.item}
                data-qa={`${SessionsTestId.Checklist}-${entry.item}`}
                secondaryAction={
                  <Button component={RouterLink} to={entry.to} size="small">
                    {t('view.goTo')}
                  </Button>
                }>
                <ListItemIcon>
                  {entry.done ? (
                    <CheckCircleIcon color="success" titleAccess={t('view.checklistDone')} />
                  ) : (
                    <ErrorOutlineIcon color="warning" titleAccess={t('view.checklistPending')} />
                  )}
                </ListItemIcon>
                <ListItemText primary={t(`view.checklist.${entry.item}`)} />
              </ListItem>
            ))}
          </List>
        </Paper>
      )}
    </Stack>
  );
};

export default SessionView;
