import { screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { DiagnosticArgument, DiagnosticCode, DiagnosticSeverity, RuleType, type DiagnosticDto, type RoomDto, type RuleDto, type TestCenterDto } from '../../../../api/dtos';
import { apiRoutes } from '../../../../constants/apiRoutes';
import { routes, type ParticipantsListFilter } from '../../../../constants/routes';
import { AccessibleFilter } from '../../../../enums/Participants';
import { ConflictCoreReason, DiagnosticLinkTarget, SolveRunsTestId } from '../../../../enums/SolveRuns';
import solveRunsTexts from '../../../../i18n/uk/solveRuns.json';
import { mockApiUrl } from '../../../../mocks/handlers/rooms';
import { server } from '../../../../mocks/server';
import { allByQa, byQa, renderWithProviders } from '../../../../test-utils';
import DiagnosticsPanel from '../Common/DiagnosticsPanel';
import { DIAGNOSTIC_LINK_TARGETS } from '../Common/diagnosticLinks';

const SESSION_ID = 'diag-session';
const CENTER_ID = 'center-a';
const ROOM_ID = 'room-101';
const RULE_1 = 'rule-1';
const RULE_2 = 'rule-2';

const CENTERS: TestCenterDto[] = [{ id: CENTER_ID, name: 'Ліцей №1' }];
const ROOMS: RoomDto[] = [{ id: ROOM_ID, name: '101', centerId: CENTER_ID }];
const RULES: RuleDto[] = [
  { id: RULE_1, type: RuleType.SeparateByAttribute, displayName: 'Однокласники окремо' },
  { id: RULE_2, type: RuleType.NoAdjacentByAttribute, displayName: 'Не поруч' },
];

interface ExpectedLink {
  target: DiagnosticLinkTarget;
  href: string;
  label: string;
}

interface Case {
  diagnostic: Omit<DiagnosticDto, 'code'>;
  links: ExpectedLink[];
}

const link = (target: DiagnosticLinkTarget, href: string, label: string): ExpectedLink => ({ target, href, label });
const centerRooms = link(DiagnosticLinkTarget.CenterRooms, routes.rooms.list(SESSION_ID, CENTER_ID), 'Аудиторії центру «Ліцей №1»');
const centersList = link(DiagnosticLinkTarget.CentersList, routes.centers.list(SESSION_ID), 'Центри та аудиторії');
const participants = (target: DiagnosticLinkTarget, label: string, filter: ParticipantsListFilter = {}) => link(target, routes.participants.list(SESSION_ID, filter), label);
const roomEdit = link(DiagnosticLinkTarget.RoomEdit, routes.rooms.edit(SESSION_ID, CENTER_ID, ROOM_ID), 'План аудиторії «101»');
const rule1 = link(DiagnosticLinkTarget.Rule, routes.rules.edit(SESSION_ID, RULE_1), 'Правило «Однокласники окремо»');
const rule2 = link(DiagnosticLinkTarget.Rule, routes.rules.edit(SESSION_ID, RULE_2), 'Правило «Не поруч»');
const rulesList = link(DiagnosticLinkTarget.RulesList, routes.rules.list(SESSION_ID), 'Усі правила');
const pinsPage = link(DiagnosticLinkTarget.Pins, routes.pins.list(SESSION_ID), 'Зафіксовані учасники');
const groupValue = participants(DiagnosticLinkTarget.ParticipantsByAttribute, 'Учасники: Група = «11-А»', { group: '11-А' });

// Exhaustive like the production table: a new DiagnosticCode fails typecheck here too.
const CASES: Record<DiagnosticCode, Case> = {
  [DiagnosticCode.CapacityTotalExceeded]: {
    diagnostic: { arguments: { Size: '412', Limit: '380' } },
    links: [centersList, participants(DiagnosticLinkTarget.Participants, 'Список учасників')],
  },
  [DiagnosticCode.AccessibleCapacityExceeded]: {
    diagnostic: { ruleId: RULE_1, arguments: { Size: '5', Limit: '2' } },
    links: [centersList, participants(DiagnosticLinkTarget.ParticipantsAccessible, 'Учасники, яким потрібне доступне місце', { accessible: AccessibleFilter.Yes }), rule1],
  },
  [DiagnosticCode.SeparationImpossible]: {
    diagnostic: { ruleId: RULE_1, arguments: { AttributeKey: 'group', Value: '11-А', Size: '40', Limit: '30' } },
    links: [rule1, groupValue],
  },
  [DiagnosticCode.NoAdjacencyImpossible]: {
    diagnostic: { ruleId: RULE_2, arguments: { AttributeKey: 'group', Value: '11-А', Size: '40', Limit: '30' } },
    links: [rule2, groupValue, centersList],
  },
  [DiagnosticCode.PinInvalidSeat]: {
    diagnostic: { arguments: { ParticipantId: 'p-1', RoomId: ROOM_ID, SeatKey: 'r1c1s1' } },
    links: [pinsPage, roomEdit],
  },
  [DiagnosticCode.PinDuplicateSeat]: {
    diagnostic: { arguments: { RoomId: ROOM_ID, SeatKey: 'r1c1s1', Count: '2' } },
    links: [pinsPage, roomEdit],
  },
  // A room the client cannot resolve to a center degrades to the centers page.
  [DiagnosticCode.PinRoomOverfull]: {
    diagnostic: { arguments: { RoomId: 'deleted-room', Size: '31', Limit: '30' } },
    links: [pinsPage, centersList],
  },
  [DiagnosticCode.PinRuleConflict]: {
    diagnostic: { ruleId: RULE_1, arguments: { AttributeKey: 'group', Value: '11-А', Size: '3', Limit: '2' } },
    links: [rule1, pinsPage, groupValue],
  },
  [DiagnosticCode.AttributeMissing]: {
    diagnostic: { severity: DiagnosticSeverity.Warning, arguments: { AttributeKey: 'school', Count: '3' } },
    links: [
      participants(DiagnosticLinkTarget.ParticipantsByAttribute, 'Учасники за атрибутом «Навчальний заклад»'),
      link(DiagnosticLinkTarget.ParticipantsImport, routes.participants.import(SESSION_ID), 'Імпорт учасників'),
    ],
  },
  [DiagnosticCode.NoActiveRooms]: { diagnostic: {}, links: [centersList] },
  [DiagnosticCode.ModelTooLarge]: { diagnostic: { arguments: { Size: '9000000', Limit: '5000000' } }, links: [rulesList] },
  [DiagnosticCode.SeatingInfeasible]: {
    diagnostic: { arguments: { RoomId: ROOM_ID, AttributeKey: 'group', Value: '11-А', Size: '20' } },
    links: [roomEdit, groupValue, rulesList],
  },
  [DiagnosticCode.ConflictCore]: {
    diagnostic: { arguments: { RuleIds: `${RULE_1},${RULE_2}`, CenterId: CENTER_ID } },
    links: [rule1, rule2, centerRooms],
  },
  [DiagnosticCode.AlternativesLimited]: { diagnostic: { severity: DiagnosticSeverity.Warning, arguments: { Reason: 'TimeBudgetExhausted', Count: '2', Limit: '5' } }, links: [] },
  [DiagnosticCode.InternalValidationFailed]: { diagnostic: { ruleId: RULE_1, arguments: { Count: '1' } }, links: [] },
};

beforeEach(() => {
  server.use(
    http.get(mockApiUrl(apiRoutes.centers.list(':sessionId')), () => HttpResponse.json(CENTERS)),
    http.get(mockApiUrl(apiRoutes.rooms.list(':centerId')), () => HttpResponse.json(ROOMS))
  );
});

const renderPanel = (diagnostic: DiagnosticDto) =>
  renderWithProviders(<DiagnosticsPanel diagnostics={[{ severity: DiagnosticSeverity.Error, message: 'Повідомлення', ...diagnostic }]} sessionId={SESSION_ID} rules={RULES} />);

describe('DiagnosticsPanel suggestions and deep links', () => {
  it.each(Object.values(DiagnosticCode))('%s: shows its suggestion and links to where to fix it', async (code) => {
    const { diagnostic, links } = CASES[code];
    const { container } = renderPanel({ ...diagnostic, code });

    const item = byQa(container, SolveRunsTestId.DiagnosticItem) as HTMLElement;
    expect(item).toHaveAttribute('data-code', code);
    expect(within(item).getByText(solveRunsTexts.diagnostics.code[code])).toBeInTheDocument();
    expect(byQa(item, SolveRunsTestId.DiagnosticSuggestion)?.textContent).toBe(`Що зробити: ${solveRunsTexts.diagnostics.suggestion[code]}`);

    // Room and center names arrive asynchronously; wait for the final link set.
    await waitFor(() =>
      expect(
        allByQa(item, SolveRunsTestId.DiagnosticLink).map((element) => ({
          target: element.getAttribute('data-target'),
          href: element.getAttribute('href'),
          label: element.textContent,
        }))
      ).toEqual(links)
    );
    expect(DIAGNOSTIC_LINK_TARGETS[code].length === 0).toBe(links.length === 0);
  });

  it('explains an empty diagnostics list when asked to (Infeasible without a conflict core)', () => {
    renderWithProviders(<DiagnosticsPanel diagnostics={[]} sessionId={SESSION_ID} emptyText="Причину не визначено." />);
    expect(screen.getByText(/Причину не визначено/)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Усі правила' })).toHaveAttribute('href', routes.rules.list(SESSION_ID));
    expect(screen.queryByText('Проблем не виявлено')).not.toBeInTheDocument();
  });

  it('notes an unminimized conflict core (proof timed out, every hard rule listed)', () => {
    const { container } = renderPanel({ code: DiagnosticCode.ConflictCore, arguments: { [DiagnosticArgument.Reason]: ConflictCoreReason.Unminimized } });
    expect(byQa(container, SolveRunsTestId.DiagnosticUnminimized)).toHaveTextContent(solveRunsTexts.diagnostics.unminimized);
  });

  it('shows no unminimized note for a minimal conflict core', () => {
    const { container } = renderPanel({ code: DiagnosticCode.ConflictCore, arguments: { RuleIds: RULE_1 } });
    expect(byQa(container, SolveRunsTestId.DiagnosticUnminimized)).toBeNull();
  });

  it('falls back to generic labels when rule and center names are unknown', async () => {
    renderWithProviders(
      <DiagnosticsPanel diagnostics={[{ code: DiagnosticCode.ConflictCore, arguments: { RuleIds: 'missing-rule', CenterId: 'missing-center' } }]} sessionId={SESSION_ID} />
    );
    expect(await screen.findByRole('link', { name: 'Перейти до правила' })).toHaveAttribute('href', routes.rules.edit(SESSION_ID, 'missing-rule'));
    expect(screen.getByRole('link', { name: 'Перейти до центру' })).toHaveAttribute('href', routes.rooms.list(SESSION_ID, 'missing-center'));
  });
});
