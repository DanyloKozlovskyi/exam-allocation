import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { AdjacencyKind, RuleType, type NoAdjacentByAttributeParametersDto } from '../../../../api/dtos';
import { RoutePath, routes } from '../../../../constants/routes';
import { RulesTestId } from '../../../../enums/Rules';
import { SolveRunsTestId } from '../../../../enums/SolveRuns';
import { apiRoutes } from '../../../../constants/apiRoutes';
import { getMockRules, MOCK_ATTRIBUTES, resetRulesMocks } from '../../../../mocks/handlers/rules';
import { mockApiUrl } from '../../../../mocks/handlers/rooms';
import { server } from '../../../../mocks/server';
import { resetSolveRunsMocks } from '../../../../mocks/handlers/solveRuns';
import RulesCreateView from '../../../../views/Rules/Create';
import RulesEditView from '../../../../views/Rules/Edit';
import RulesListView from '../../../../views/Rules/List';
import { allByQa, byQa, renderWithProviders } from '../../SolveRuns/Common/testUtils/renderWithProviders';
import { PARAMETER_FORMS } from '../Common/components/ParameterForms';
import { defaultParametersState, toParametersDto } from '../Common/parameters';

const SESSION_ID = 'rules-session';

beforeEach(() => {
  // Own attribute fixture, independent of which feature's handler serves the endpoint.
  server.use(http.get(mockApiUrl(apiRoutes.participants.attributes(':sessionId')), () => HttpResponse.json(MOCK_ATTRIBUTES)));
  resetRulesMocks();
  resetSolveRunsMocks();
});

const pickOption = async (comboboxName: string, optionName: string) => {
  fireEvent.mouseDown(screen.getByRole('combobox', { name: comboboxName }));
  fireEvent.click(await screen.findByRole('option', { name: optionName }));
};

describe('rule parameters', () => {
  it('has a parameters form and a typed DTO builder for every rule type', () => {
    Object.values(RuleType).forEach((type) => {
      expect(PARAMETER_FORMS[type]).toBeDefined();
      expect(toParametersDto(type, defaultParametersState).type).toBe(type);
    });
  });
});

describe('Rules list', () => {
  it('lists rules, toggles enabled and runs the precheck', async () => {
    const { container } = renderWithProviders(<RulesListView />, { route: routes.rules.list(SESSION_ID), path: RoutePath.Rules });

    expect(await screen.findByText('Однокласники не поруч')).toBeInTheDocument();
    expect(screen.getAllByText('Розділяти за атрибутом').length).toBeGreaterThan(0);
    expect(screen.getAllByText("Обов'язкове").length).toBeGreaterThan(0);

    fireEvent.click(screen.getByLabelText('Увімкнене: Заповнення з перших рядів'));
    expect(await screen.findByText('Правило увімкнено')).toBeInTheDocument();
    await waitFor(() => expect(getMockRules(SESSION_ID)[2].isEnabled).toBe(true));

    fireEvent.click(byQa(container, RulesTestId.PrecheckButton)!);
    await waitFor(() => expect(byQa(container, SolveRunsTestId.DiagnosticsPanel)).not.toBeNull());
    const panel = byQa(container, SolveRunsTestId.DiagnosticsPanel) as HTMLElement;
    expect(within(panel).getByText('Відсутні значення атрибута')).toBeInTheDocument();
    expect(within(panel).getByText(/Доповніть дані учасників/)).toBeInTheDocument();
    expect(allByQa(panel, SolveRunsTestId.DiagnosticItem)).toHaveLength(1);
  });

  it('deletes a rule after confirmation', async () => {
    const { container } = renderWithProviders(<RulesListView />, { route: routes.rules.list(SESSION_ID), path: RoutePath.Rules });
    await screen.findByText('Однокласники не поруч');
    fireEvent.click(allByQa(container, RulesTestId.DeleteButton)[0]);
    fireEvent.click(await screen.findByRole('button', { name: 'Підтвердити' }));
    expect(await screen.findByText('Правило видалено')).toBeInTheDocument();
    expect(getMockRules(SESSION_ID)).toHaveLength(2);
  });
});

describe('Rule create', () => {
  it('creates a soft NoAdjacentByAttribute rule with typed parameters', async () => {
    const user = userEvent.setup();
    const { container } = renderWithProviders(<RulesCreateView />, { route: routes.rules.create(SESSION_ID), path: RoutePath.RuleCreate });

    await screen.findByRole('combobox', { name: 'Тип правила' });
    await pickOption('Тип правила', 'Не садити поруч однакових');
    await user.click(screen.getByLabelText('Бажане'));
    const weight = byQa(container, RulesTestId.WeightField) as HTMLInputElement;
    fireEvent.change(weight, { target: { value: '5000' } });
    await user.click(screen.getByRole('button', { name: 'Далі' }));
    expect(screen.getByText('Від 1 до 1000')).toBeInTheDocument();
    fireEvent.change(weight, { target: { value: '200' } });
    await user.click(screen.getByRole('button', { name: 'Далі' }));

    // Defaults from rule-types: group + SameDesk/Side/FrontBack.
    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Атрибут' })).toHaveTextContent('Група (12 значень)'));
    await user.click(screen.getByLabelText('За однією партою'));
    await user.click(screen.getByLabelText('Сусідні парти в ряду'));
    await user.click(screen.getByLabelText('Спереду / позаду'));
    await user.click(screen.getByRole('button', { name: 'Далі' }));
    expect(screen.getByText('Оберіть хоча б один вид сусідства')).toBeInTheDocument();

    await user.click(screen.getByLabelText('По діагоналі'));
    await pickOption('Атрибут', 'language (2 значень)');
    await user.click(screen.getByRole('button', { name: 'Далі' }));
    expect(within(byQa(container, RulesTestId.Summary) as HTMLElement).getByText('По діагоналі')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Створити правило' }));
    expect(await screen.findByText(routes.rules.list(SESSION_ID))).toBeInTheDocument();
    const created = getMockRules(SESSION_ID)[3];
    expect(created).toMatchObject({ type: RuleType.NoAdjacentByAttribute, isHard: false, weight: 200 });
    expect(created.parameters as NoAdjacentByAttributeParametersDto).toMatchObject({
      type: RuleType.NoAdjacentByAttribute,
      attributeKey: 'language',
      adjacency: [AdjacencyKind.Diagonal],
    });
  });

  it('switches to the no-parameters form and forces hard for hard-only types', async () => {
    const user = userEvent.setup();
    const { container } = renderWithProviders(<RulesCreateView />, { route: routes.rules.create(SESSION_ID), path: RoutePath.RuleCreate });
    await screen.findByRole('combobox', { name: 'Тип правила' });
    await pickOption('Тип правила', 'Місця для осіб з інвалідністю');
    expect(screen.getByLabelText('Бажане')).toBeDisabled();
    await user.click(screen.getByRole('button', { name: 'Далі' }));
    expect(byQa(container, RulesTestId.NoParameters)).toBeInTheDocument();
  });
});

describe('Rule edit', () => {
  it('loads the rule with a locked type and saves changes', async () => {
    const user = userEvent.setup();
    const rule = getMockRules(SESSION_ID)[1];
    const { container } = renderWithProviders(<RulesEditView />, { route: routes.rules.edit(SESSION_ID, rule.id!), path: RoutePath.RuleEdit });

    const type = await screen.findByRole('combobox', { name: 'Тип правила' });
    expect(type).toHaveTextContent('Розділяти за атрибутом');
    expect(type).toHaveAttribute('aria-disabled', 'true');
    await user.click(screen.getByRole('button', { name: 'Далі' }));
    fireEvent.change(byQa(container, RulesTestId.MaxPerScopeField) as HTMLInputElement, { target: { value: '2' } });
    await pickOption('Межа', 'Центр тестування');
    await user.click(screen.getByRole('button', { name: 'Далі' }));
    await user.click(screen.getByRole('button', { name: 'Зберегти правило' }));

    expect(await screen.findByText('Правило збережено')).toBeInTheDocument();
    expect(getMockRules(SESSION_ID)[1].parameters).toMatchObject({ maxPerScope: 2, scope: 'Center' });
  });
});
