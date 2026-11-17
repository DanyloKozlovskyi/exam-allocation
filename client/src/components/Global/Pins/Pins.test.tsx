import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { routes } from '../../../constants/routes';
import { PinsTestId } from '../../../enums/Pins';
import { resources, Language } from '../../../i18n';
import { getMockPins, storeMockPins } from '../../../mocks/handlers/pins';
import { MOCK_SESSION_KYIV_ID, resetMockStore } from '../../../mocks/handlers/sessions';
import { renderWithProviders } from '../../../test-utils';
import PinsList from './List';

const texts = resources[Language.Ukrainian].pins;
const common = resources[Language.Ukrainian].common;
const CENTER_ID = 'center-1';
const ROOM_ID = 'room-1';

const byQa = (id: string) => document.querySelector(`[data-qa="${id}"]`) as HTMLElement;

describe('Pins list', () => {
  beforeEach(() => {
    resetMockStore();
    storeMockPins(MOCK_SESSION_KYIV_ID, [
      { id: 'pin-1', participantId: 'p-1', roomId: ROOM_ID, seatKey: 'r0c1s0', participantName: 'Іваненко Іван', roomName: 'Ауд. 101', centerId: CENTER_ID },
      { id: 'pin-2', participantId: 'p-2', roomId: ROOM_ID, seatKey: null, participantName: 'Петренко Петро', roomName: 'Ауд. 101', centerId: CENTER_ID },
    ]);
  });

  it('lists pins with participant, room link and readable seat', async () => {
    renderWithProviders(<PinsList sessionId={MOCK_SESSION_KYIV_ID} />);

    expect(await screen.findByText('Іваненко Іван')).toBeInTheDocument();
    expect(screen.getByText('Ряд 1, парта 2, місце 1')).toBeInTheDocument();
    expect(screen.getByText(texts.seatAny)).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: 'Ауд. 101' })[0]).toHaveAttribute('href', routes.rooms.edit(MOCK_SESSION_KYIV_ID, CENTER_ID, ROOM_ID));
  });

  it('removes one pin after confirmation', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PinsList sessionId={MOCK_SESSION_KYIV_ID} />);
    await screen.findByText('Іваненко Іван');

    await user.click(within(byQa(PinsTestId.List)).getAllByRole('button', { name: texts.actions.remove })[0]);
    await user.click(screen.getByRole('button', { name: common.actions.confirm }));

    expect(await screen.findByText(texts.messages.removed)).toBeInTheDocument();
    await waitFor(() => expect(getMockPins(MOCK_SESSION_KYIV_ID)).toHaveLength(1));
  });

  it('removes all pins after confirmation', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PinsList sessionId={MOCK_SESSION_KYIV_ID} />);
    await screen.findByText('Іваненко Іван');

    await user.click(byQa(PinsTestId.RemoveAllButton));
    await user.click(screen.getByRole('button', { name: common.actions.confirm }));

    expect(await screen.findByText(texts.messages.removedAll)).toBeInTheDocument();
    expect(getMockPins(MOCK_SESSION_KYIV_ID)).toHaveLength(0);
    await waitFor(() => expect(byQa(PinsTestId.RemoveAllButton)).toBeDisabled());
  });
});
