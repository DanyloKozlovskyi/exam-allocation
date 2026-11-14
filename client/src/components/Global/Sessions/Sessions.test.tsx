import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SessionChecklistItem, SessionsTestId } from '../../../enums/Sessions';
import { resources, Language } from '../../../i18n';
import { MOCK_SESSION_EMPTY_ID, MOCK_SESSION_KYIV_ID, resetMockStore } from '../../../mocks/handlers/sessions';
import { renderWithProviders } from './Common/testing';
import SessionCreate from './Create';
import SessionEdit from './Edit';
import SessionsList from './List';
import SessionView from './View';

const texts = resources[Language.Ukrainian].sessions;
const common = resources[Language.Ukrainian].common;

const byQa = (id: string) => document.querySelector(`[data-qa="${id}"]`) as HTMLElement;

describe('Sessions', () => {
  beforeEach(() => resetMockStore());

  it('lists sessions from the API', async () => {
    renderWithProviders(<SessionsList />);

    expect(await screen.findByText('НМТ 2026 — Київ, основна сесія')).toBeInTheDocument();
    expect(screen.getByText('Пробне тестування 2026')).toBeInTheDocument();
  });

  it('creates a session through Details → Summary and validates the name', async () => {
    const user = userEvent.setup();
    const onCreated = jest.fn();
    renderWithProviders(<SessionCreate onCreated={onCreated} />);

    await user.click(screen.getByRole('button', { name: common.actions.next }));
    expect(screen.getByText(texts.validation.required)).toBeInTheDocument();

    await user.type(byQa(SessionsTestId.NameField), 'НМТ 2026 — Одеса');
    await user.type(byQa(SessionsTestId.ExamDateField), '2026-06-20');
    await user.click(screen.getByRole('button', { name: common.actions.next }));

    const review = byQa(SessionsTestId.Review);
    expect(within(review).getByText('НМТ 2026 — Одеса')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: texts.form.createAction }));

    await waitFor(() => expect(onCreated).toHaveBeenCalled());
    expect(onCreated.mock.calls[0][0]).toMatchObject({ name: 'НМТ 2026 — Одеса', examDate: '2026-06-20' });
    expect(await screen.findByText(texts.messages.created)).toBeInTheDocument();
  });

  it('shows a conflict error when the name is taken', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SessionCreate onCreated={jest.fn()} />);

    await user.type(byQa(SessionsTestId.NameField), 'Пробне тестування 2026');
    await user.click(screen.getByRole('button', { name: common.actions.next }));
    await user.click(screen.getByRole('button', { name: texts.form.createAction }));

    expect(await screen.findByText(common.errors.SessionAlreadyExists)).toBeInTheDocument();
  });

  it('edits an existing session', async () => {
    const user = userEvent.setup();
    const onSaved = jest.fn();
    renderWithProviders(<SessionEdit sessionId={MOCK_SESSION_KYIV_ID} onSaved={onSaved} />);

    const name = await waitFor(() => {
      const field = byQa(SessionsTestId.NameField) as HTMLInputElement;
      expect(field).toHaveValue('НМТ 2026 — Київ, основна сесія');
      return field;
    });
    await user.clear(name);
    await user.type(name, 'НМТ 2026 — Київ');
    await user.click(screen.getByRole('button', { name: common.actions.next }));
    await user.click(screen.getByRole('button', { name: texts.form.saveAction }));

    await waitFor(() => expect(onSaved).toHaveBeenCalledWith(expect.objectContaining({ name: 'НМТ 2026 — Київ', version: 2 })));
  });

  it('shows the dashboard with totals and the checklist', async () => {
    renderWithProviders(<SessionView sessionId={MOCK_SESSION_KYIV_ID} />);

    expect(await screen.findByText('НМТ 2026 — Київ, основна сесія')).toBeInTheDocument();
    await waitFor(() => expect(byQa(`${SessionsTestId.Totals}-participants`)).toHaveTextContent('120'));
    expect(byQa(`${SessionsTestId.Totals}-centers`)).toHaveTextContent('3');

    const checklist = byQa(SessionsTestId.Checklist);
    expect(within(checklist).getByText(texts.view.checklist.Participants)).toBeInTheDocument();
    expect(within(byQa(`${SessionsTestId.Checklist}-${SessionChecklistItem.Participants}`)).getByTitle(texts.view.checklistDone)).toBeInTheDocument();
    expect(within(byQa(`${SessionsTestId.Checklist}-${SessionChecklistItem.Rules}`)).getByTitle(texts.view.checklistPending)).toBeInTheDocument();
  });

  it('marks everything pending for an empty session', async () => {
    renderWithProviders(<SessionView sessionId={MOCK_SESSION_EMPTY_ID} />);

    await waitFor(() => expect(byQa(SessionsTestId.Checklist)).toBeInTheDocument());
    expect(within(byQa(`${SessionsTestId.Checklist}-${SessionChecklistItem.Centers}`)).getByTitle(texts.view.checklistPending)).toBeInTheDocument();
  });
});
