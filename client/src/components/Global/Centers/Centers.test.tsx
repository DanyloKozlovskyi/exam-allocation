import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes, useLocation } from 'react-router-dom';
import { RoutePath, routes } from '../../../constants/routes';
import { CentersTestId } from '../../../enums/Centers';
import { resources, Language } from '../../../i18n';
import { MOCK_CENTER_KNU_ID } from '../../../mocks/handlers/centers';
import { MOCK_SESSION_KYIV_ID, resetMockStore } from '../../../mocks/handlers/sessions';
import { renderWithProviders } from '../../../test-utils';
import CenterCreate from './Create';
import CenterEdit from './Edit';
import CentersList from './List';
import CentersMap from './Map/CentersMap';

const texts = resources[Language.Ukrainian].centers;
const common = resources[Language.Ukrainian].common;

const byQa = (id: string) => document.querySelector(`[data-qa="${id}"]`) as HTMLElement;

const findGridRow = async (name: string) => {
  await screen.findAllByText(name);
  return (await within(byQa(CentersTestId.List)).findByText(name)).closest('[role="row"]') as HTMLElement;
};

const LocationProbe = () => <div data-testid="location">{useLocation().pathname}</div>;

describe('Centers', () => {
  beforeEach(() => resetMockStore());

  it('renders the coordinates fallback when no Google Maps key is configured', async () => {
    const user = userEvent.setup();
    const onAddAtPoint = jest.fn();
    const onSelect = jest.fn();
    renderWithProviders(<CentersMap centers={[{ id: 'c1', name: 'Центр 1', latitude: 50, longitude: 30, isActive: true }]} onAddAtPoint={onAddAtPoint} onSelect={onSelect} />);

    expect(byQa(CentersTestId.MapFallback)).toBeInTheDocument();
    expect(byQa(CentersTestId.Map)).toBeNull();

    await user.clear(byQa(CentersTestId.FallbackLatitude));
    await user.type(byQa(CentersTestId.FallbackLatitude), '50,4501');
    await user.clear(byQa(CentersTestId.FallbackLongitude));
    await user.type(byQa(CentersTestId.FallbackLongitude), '30.5234');
    await user.click(byQa(CentersTestId.FallbackAdd));
    expect(onAddAtPoint).toHaveBeenCalledWith({ latitude: 50.4501, longitude: 30.5234 });

    await user.click(screen.getByText('Центр 1'));
    expect(onSelect).toHaveBeenCalledWith('c1');
  });

  it('rejects out-of-range coordinates in the fallback', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CentersMap centers={[]} onAddAtPoint={jest.fn()} />);

    await user.clear(byQa(CentersTestId.FallbackLatitude));
    await user.type(byQa(CentersTestId.FallbackLatitude), '120');
    expect(screen.getByText(texts.validation.outOfRange)).toBeInTheDocument();
    expect(byQa(CentersTestId.FallbackAdd)).toBeDisabled();
  });

  it('lists centers with room count and capacity and opens Create from a map point', async () => {
    const user = userEvent.setup();
    const onCreate = jest.fn();
    renderWithProviders(<CentersList sessionId={MOCK_SESSION_KYIV_ID} onCreate={onCreate} />);

    const row = await findGridRow('КНУ імені Тараса Шевченка, корпус 1');
    expect(within(row).getByText('12')).toBeInTheDocument();
    expect(within(row).getByText(/360/)).toBeInTheDocument();

    await user.click(byQa(CentersTestId.FallbackAdd));
    expect(onCreate).toHaveBeenCalledWith({ latitude: 49, longitude: 31.4 });
  });

  it('links each center to its rooms list', async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <Routes>
        <Route path={RoutePath.Centers} element={<CentersList sessionId={MOCK_SESSION_KYIV_ID} onCreate={jest.fn()} />} />
        <Route path={RoutePath.Rooms} element={<LocationProbe />} />
      </Routes>,
      { route: routes.centers.list(MOCK_SESSION_KYIV_ID) }
    );

    const row = await findGridRow('КНУ імені Тараса Шевченка, корпус 1');
    await user.click(within(row).getByRole('button', { name: texts.list.rooms }));

    expect(await screen.findByTestId('location')).toHaveTextContent(routes.rooms.list(MOCK_SESSION_KYIV_ID, MOCK_CENTER_KNU_ID));
  });

  it('toggles the active flag and deletes a center after confirmation', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CentersList sessionId={MOCK_SESSION_KYIV_ID} onCreate={jest.fn()} />);

    const row = await findGridRow('Ліцей № 171 «Лідер»');
    await user.click(within(row).getByRole('switch'));
    expect(await screen.findByText(texts.messages.activated)).toBeInTheDocument();

    await user.click(within(row).getByRole('button', { name: texts.list.deleteTitle }));
    await user.click(screen.getByRole('button', { name: common.actions.confirm }));

    expect(await screen.findByText(texts.messages.deleted)).toBeInTheDocument();
    await waitFor(() => expect(within(byQa(CentersTestId.List)).queryByText('Ліцей № 171 «Лідер»')).not.toBeInTheDocument());
  });

  it('creates a center prefilled from a picked place', async () => {
    const user = userEvent.setup();
    const onCreated = jest.fn();
    renderWithProviders(
      <CenterCreate
        sessionId={MOCK_SESSION_KYIV_ID}
        prefill={{ name: 'Ліцей № 100 «Поділ»', address: 'вул. Хорива, 3, Київ', latitude: 50.4652, longitude: 30.5165, placeId: 'place-1' }}
        onCreated={onCreated}
      />
    );

    expect(byQa(CentersTestId.NameField)).toHaveValue('Ліцей № 100 «Поділ»');
    expect(byQa(CentersTestId.LatitudeField)).toHaveValue('50.4652');

    await user.click(screen.getByRole('button', { name: common.actions.next }));
    expect(within(byQa(CentersTestId.Review)).getByText('place-1')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: texts.form.createAction }));

    await waitFor(() => expect(onCreated).toHaveBeenCalled());
    expect(onCreated.mock.calls[0][0]).toMatchObject({ name: 'Ліцей № 100 «Поділ»', latitude: 50.4652, longitude: 30.5165, googlePlaceId: 'place-1', roomCount: 0 });
  });

  it('validates required fields and coordinates on Create', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CenterCreate sessionId={MOCK_SESSION_KYIV_ID} onCreated={jest.fn()} />);

    await user.type(byQa(CentersTestId.LatitudeField), 'abc');
    await user.click(screen.getByRole('button', { name: common.actions.next }));

    expect(screen.getAllByText(texts.validation.required).length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText(texts.validation.notANumber)).toBeInTheDocument();
  });

  it('edits a center', async () => {
    const user = userEvent.setup();
    const onSaved = jest.fn();
    renderWithProviders(<CenterEdit sessionId={MOCK_SESSION_KYIV_ID} centerId={MOCK_CENTER_KNU_ID} onSaved={onSaved} />);

    await waitFor(() => expect(byQa(CentersTestId.NameField)).toHaveValue('КНУ імені Тараса Шевченка, корпус 1'));
    await user.clear(byQa(CentersTestId.CostField));
    await user.type(byQa(CentersTestId.CostField), '2');
    await user.click(screen.getByRole('button', { name: common.actions.next }));
    await user.click(screen.getByRole('button', { name: texts.form.saveAction }));

    await waitFor(() => expect(onSaved).toHaveBeenCalledWith(expect.objectContaining({ costPerRoom: 2, roomCount: 12, version: 2 })));
  });
});
