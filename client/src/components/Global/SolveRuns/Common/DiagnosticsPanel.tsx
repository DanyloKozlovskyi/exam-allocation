import { Alert, AlertTitle, Link, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import { DiagnosticSeverity, type DiagnosticDto, type RuleDto } from '../../../../api/dtos';
import { routes } from '../../../../constants/routes';
import { SolveRunsTestId } from '../../../../enums/SolveRuns';
import { I18nNamespace } from '../../../../i18n';
import { DIAGNOSTIC_SEVERITY_COLOR } from './constants';

interface Props {
  diagnostics: DiagnosticDto[];
  sessionId: string;
  /** Used to name the offending rule; optional. */
  rules?: RuleDto[];
  title?: string;
}

/** Shared by Rules (precheck) and Solve (run diagnostics): message, links, suggestion per DiagnosticCode. */
const DiagnosticsPanel = ({ diagnostics, sessionId, rules = [], title }: Props) => {
  const { t } = useTranslation(I18nNamespace.SolveRuns);
  const { t: tr } = useTranslation(I18nNamespace.Rules);
  const ruleName = (ruleId: string) => {
    const rule = rules.find((item) => item.id === ruleId);
    return rule ? rule.displayName || (rule.type ? tr(`ruleType.${rule.type}`) : ruleId) : t('diagnostics.ruleLink');
  };

  return (
    <Stack spacing={1} data-qa={SolveRunsTestId.DiagnosticsPanel}>
      <Typography variant="h6">{title ?? t('diagnostics.title')}</Typography>
      {diagnostics.length === 0 && <Alert severity="success">{t('diagnostics.empty')}</Alert>}
      {diagnostics.map((diagnostic, index) => {
        const severity = diagnostic.severity ?? DiagnosticSeverity.Error;
        const centerId = diagnostic.arguments?.CenterId;
        return (
          <Alert key={`${diagnostic.code}-${index}`} severity={DIAGNOSTIC_SEVERITY_COLOR[severity]} data-qa={SolveRunsTestId.DiagnosticItem} data-code={diagnostic.code}>
            <AlertTitle>{diagnostic.code ? t(`diagnostics.code.${diagnostic.code}`) : t(`diagnostics.severity.${severity}`)}</AlertTitle>
            {diagnostic.message && <Typography variant="body2">{diagnostic.message}</Typography>}
            {diagnostic.code && (
              <Typography variant="body2" sx={{ mt: 0.5 }}>
                <strong>{t('diagnostics.suggestionLabel')}: </strong>
                {t(`diagnostics.suggestion.${diagnostic.code}`)}
              </Typography>
            )}
            <Stack direction="row" spacing={2} sx={{ mt: 0.5 }}>
              {diagnostic.ruleId && (
                <Link component={RouterLink} to={routes.rules.edit(sessionId, diagnostic.ruleId)}>
                  {ruleName(diagnostic.ruleId)}
                </Link>
              )}
              {centerId && (
                <Link component={RouterLink} to={routes.centers.edit(sessionId, centerId)}>
                  {t('diagnostics.centerLink')}
                </Link>
              )}
            </Stack>
          </Alert>
        );
      })}
    </Stack>
  );
};

export default DiagnosticsPanel;
