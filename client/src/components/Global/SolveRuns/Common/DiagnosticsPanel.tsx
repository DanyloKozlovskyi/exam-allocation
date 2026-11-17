import { Alert, AlertTitle, Link, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import { DiagnosticArgument, DiagnosticCode, DiagnosticSeverity, type DiagnosticDto, type RuleDto } from '../../../../api/dtos';
import { routes } from '../../../../constants/routes';
import { ConflictCoreReason, DiagnosticLinkTarget, SolveRunsTestId } from '../../../../enums/SolveRuns';
import { I18nNamespace } from '../../../../i18n';
import { useAttributeLabel } from '../../Rules/Common/useAttributeLabel';
import { DIAGNOSTIC_SEVERITY_COLOR } from './constants';
import { buildDiagnosticLinks, DiagnosticLinkLabelArg, type DiagnosticLink } from './diagnosticLinks';
import { useDiagnosticRooms } from './useDiagnosticRooms';

interface Props {
  diagnostics: DiagnosticDto[];
  sessionId: string;
  /** Used to name the offending rule; optional. */
  rules?: RuleDto[];
  title?: string;
  /** Shown instead of "no problems" when the list is empty (e.g. an Infeasible run without an explanation, R-21). */
  emptyText?: string;
}

/** Shared by Rules (precheck) and Solve (run diagnostics): message, suggestion and deep links per DiagnosticCode. */
const DiagnosticsPanel = ({ diagnostics, sessionId, rules = [], title, emptyText }: Props) => {
  const { t } = useTranslation(I18nNamespace.SolveRuns);
  const { t: tr } = useTranslation(I18nNamespace.Rules);
  const attributeLabel = useAttributeLabel();
  const { rooms, centerNames } = useDiagnosticRooms(sessionId, diagnostics);

  const ruleName = (ruleId: string) => {
    const rule = rules.find((item) => item.id === ruleId);
    if (!rule) return undefined;
    return rule.displayName || (rule.type ? tr(`ruleType.${rule.type}`) : undefined);
  };

  const linkLabel = ({ target, labelArgs }: DiagnosticLink) => {
    const name = labelArgs[DiagnosticLinkLabelArg.Name] ?? '';
    const attribute = attributeLabel(labelArgs[DiagnosticLinkLabelArg.Attribute] ?? '');
    const value = labelArgs[DiagnosticLinkLabelArg.Value];
    switch (target) {
      case DiagnosticLinkTarget.Rule: {
        const resolved = ruleName(name);
        return resolved ? t('diagnostics.link.Rule', { name: resolved }) : t('diagnostics.ruleLink');
      }
      case DiagnosticLinkTarget.CenterRooms: {
        const resolved = centerNames.get(name);
        return resolved ? t('diagnostics.link.CenterRooms', { name: resolved }) : t('diagnostics.centerLink');
      }
      case DiagnosticLinkTarget.ParticipantsByAttribute:
        return value ? t('diagnostics.attributeValueLink', { attribute, value }) : t('diagnostics.link.ParticipantsByAttribute', { attribute });
      default:
        return t(`diagnostics.link.${target}`, { name });
    }
  };

  return (
    <Stack spacing={1} data-qa={SolveRunsTestId.DiagnosticsPanel}>
      <Typography variant="h6">{title ?? t('diagnostics.title')}</Typography>
      {diagnostics.length === 0 &&
        (emptyText ? (
          <Alert severity="warning">
            {emptyText}{' '}
            <Link component={RouterLink} to={routes.rules.list(sessionId)}>
              {t('diagnostics.link.RulesList')}
            </Link>
          </Alert>
        ) : (
          <Alert severity="success">{t('diagnostics.empty')}</Alert>
        ))}
      {diagnostics.map((diagnostic, index) => {
        const severity = diagnostic.severity ?? DiagnosticSeverity.Error;
        const links = buildDiagnosticLinks(diagnostic, { sessionId, rooms });
        return (
          <Alert key={`${diagnostic.code}-${index}`} severity={DIAGNOSTIC_SEVERITY_COLOR[severity]} data-qa={SolveRunsTestId.DiagnosticItem} data-code={diagnostic.code}>
            <AlertTitle>{diagnostic.code ? t(`diagnostics.code.${diagnostic.code}`) : t(`diagnostics.severity.${severity}`)}</AlertTitle>
            {diagnostic.message && <Typography variant="body2">{diagnostic.message}</Typography>}
            {diagnostic.code === DiagnosticCode.ConflictCore && diagnostic.arguments?.[DiagnosticArgument.Reason] === ConflictCoreReason.Unminimized && (
              <Typography variant="body2" sx={{ mt: 0.5 }} data-qa={SolveRunsTestId.DiagnosticUnminimized}>
                {t('diagnostics.unminimized')}
              </Typography>
            )}
            {diagnostic.code && (
              <Typography variant="body2" sx={{ mt: 0.5 }} data-qa={SolveRunsTestId.DiagnosticSuggestion}>
                <strong>{t('diagnostics.suggestionLabel')}: </strong>
                {t(`diagnostics.suggestion.${diagnostic.code}`)}
              </Typography>
            )}
            {links.length > 0 && (
              <Stack direction="row" spacing={2} useFlexGap sx={{ mt: 0.5, flexWrap: 'wrap', alignItems: 'baseline' }}>
                <Typography variant="body2" component="span">
                  <strong>{t('diagnostics.linksLabel')}:</strong>
                </Typography>
                {links.map((link) => (
                  <Link key={link.to} component={RouterLink} to={link.to} variant="body2" data-qa={SolveRunsTestId.DiagnosticLink} data-target={link.target}>
                    {linkLabel(link)}
                  </Link>
                ))}
              </Stack>
            )}
          </Alert>
        );
      })}
    </Stack>
  );
};

export default DiagnosticsPanel;
