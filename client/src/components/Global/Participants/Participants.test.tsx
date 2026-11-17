import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ParticipantSortField, SortDirection } from '../../../api/dtos';
import { ParticipantsTestId } from '../../../enums/Participants';
import { resources, Language } from '../../../i18n';
import { MockAttributeKey, ParticipantsQueryParam } from '../../../mocks/handlers/participants';
import { MOCK_SESSION_KYIV_ID, resetMockStore } from '../../../mocks/handlers/sessions';
import { server } from '../../../mocks/server';
import { renderWithProviders } from '../../../test-utils';
import ParticipantsList from './List';

const texts = resources[Language.Ukrainian].participants;
const common = resources[Language.Ukrainian].common;

const byQa = (id: string) => document.querySelector(`[data-qa="${id}"]`) as HTMLElement;
const grid = () => byQa(ParticipantsTestId.Grid);
const totalText = (count: number) => texts.list.total.replace('{{count}}', String(count));

const requests: URLSearchParams[] = [];
const recordRequest = ({ request }: { request: Request }) => {
  const url = new URL(request.url);
  if (request.method === 'GET' && url.pathname.endsWith('/participants')) {
    requests.push(url.searchParams);
  }
};

const lastRequest = () => requests[requests.length - 1];

// The grid renders 50 rows with ~12 columns in jsdom; this is slow on shared CI machines.
jest.setTimeout(60000);

describe('Participants list', () => {
  beforeAll(() => server.events.on('request:start', recordRequest));
  afterAll(() => server.events.removeAllListeners());
  beforeEach(() => {
    resetMockStore();
    requests.length = 0;
  });

  it('loads the first page from the server with dynamic attribute columns', async () => {
    renderWithProviders(<ParticipantsList sessionId={MOCK_SESSION_KYIV_ID} />);

    expect(await screen.findByText(totalText(120))).toBeInTheDocument();
    expect(await within(grid()).findByRole('columnheader', { name: MockAttributeKey.Profile })).toBeInTheDocument();
    expect(lastRequest().get(ParticipantsQueryParam.Page)).toBe('1');
    expect(lastRequest().get(ParticipantsQueryParam.PageSize)).toBe('50');
    expect(within(grid()).getAllByRole('row').length).toBeGreaterThan(1);
  });

  it('sends search, filters, sorting and paging to the server', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ParticipantsList sessionId={MOCK_SESSION_KYIV_ID} />);
    await screen.findByText(totalText(120));

    await user.click(within(grid()).getByRole('columnheader', { name: texts.list.columns.group }));
    await waitFor(() => expect(lastRequest().get(ParticipantsQueryParam.SortBy)).toBe(ParticipantSortField.Group));
    expect(lastRequest().get(ParticipantsQueryParam.SortDirection)).toBe(SortDirection.Ascending);

    await user.click(screen.getByRole('button', { name: /next page|наступн/i }));
    await waitFor(() => expect(lastRequest().get(ParticipantsQueryParam.Page)).toBe('2'));

    await user.type(byQa(ParticipantsTestId.GroupFilter), '11-А');
    await waitFor(() => expect(lastRequest().get(ParticipantsQueryParam.Group)).toBe('11-А'));
    expect(lastRequest().get(ParticipantsQueryParam.Page)).toBe('1');
    expect(await screen.findByText(totalText(24))).toBeInTheDocument();

    await user.type(byQa(ParticipantsTestId.Search), 'Шевченко');
    await waitFor(() => expect(lastRequest().get(ParticipantsQueryParam.Search)).toBe('Шевченко'));
  });

  it('edits a cell inline and saves it through the API', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ParticipantsList sessionId={MOCK_SESSION_KYIV_ID} />);
    await screen.findByText(totalText(120));

    const cell = within(grid())
      .getAllByRole('gridcell')
      .find((item) => item.getAttribute('data-field') === 'group') as HTMLElement;
    await user.dblClick(cell);
    const input = within(cell).getByRole('textbox');
    await user.clear(input);
    await user.type(input, '11-Г{Enter}');

    expect(await screen.findByText(texts.messages.updated)).toBeInTheDocument();
    expect(await within(grid()).findByText('11-Г')).toBeInTheDocument();
  });

  it('rejects an inline edit that empties the last name', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ParticipantsList sessionId={MOCK_SESSION_KYIV_ID} />);
    await screen.findByText(totalText(120));

    const cell = within(grid())
      .getAllByRole('gridcell')
      .find((item) => item.getAttribute('data-field') === 'lastName') as HTMLElement;
    await user.dblClick(cell);
    await user.clear(within(cell).getByRole('textbox'));
    await user.keyboard('{Enter}');

    expect(await screen.findByText(texts.messages.nameRequired)).toBeInTheDocument();
  });

  it('deletes one participant and then all participants after confirmation', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ParticipantsList sessionId={MOCK_SESSION_KYIV_ID} />);
    await screen.findByText(totalText(120));

    await user.click(within(grid()).getAllByRole('button', { name: texts.list.deleteTitle })[0]);
    await user.click(screen.getByRole('button', { name: common.actions.confirm }));
    expect(await screen.findByText(texts.messages.deleted)).toBeInTheDocument();
    expect(await screen.findByText(totalText(119))).toBeInTheDocument();

    await user.click(byQa(ParticipantsTestId.DeleteAllButton));
    await user.click(screen.getByRole('button', { name: common.actions.confirm }));
    expect(await screen.findByText(texts.messages.deletedAll)).toBeInTheDocument();
    expect(await screen.findByText(totalText(0))).toBeInTheDocument();
  });
});
