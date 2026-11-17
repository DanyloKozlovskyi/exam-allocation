import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { DedupeStrategy, ImportMode, ParticipantField, type ImportMappingDto } from '../../../api/dtos';
import { apiRoutes } from '../../../constants/apiRoutes';
import { ImportsTestId } from '../../../enums/Imports';
import { resources, Language } from '../../../i18n';
import { MOCK_IMPORT_VALID_ROWS, MockImportHeader } from '../../../mocks/handlers/imports';
import { mockParticipantsCount } from '../../../mocks/handlers/participants';
import { MOCK_SESSION_KYIV_ID, mockUrl, resetMockStore } from '../../../mocks/handlers/sessions';
import { server } from '../../../mocks/server';
import { renderWithProviders } from '../../../test-utils';
import { checkFile, FileError, toAttributeKey } from './Common';
import ImportWizard from './Create';

const texts = resources[Language.Ukrainian].imports;
const common = resources[Language.Ukrainian].common;

const byQa = (id: string) => document.querySelector(`[data-qa="${id}"]`) as HTMLElement;
const fill = (template: string, values: Record<string, string | number>) =>
  Object.entries(values).reduce((text, [key, value]) => text.replace(`{{${key}}}`, String(value)), template);

const next = () => screen.getByRole('button', { name: common.actions.next });

const xlsxFile = () => new File(['fake'], 'participants.xlsx', { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

jest.setTimeout(60000);

describe('Import wizard', () => {
  beforeEach(() => resetMockStore());

  it('runs Upload → Sheet → Mapping → Validation → Mode → Commit end to end', async () => {
    const user = userEvent.setup();
    const onFinished = jest.fn();
    const commitBodies: ImportMappingDto[] = [];
    server.events.on('request:start', ({ request }) => {
      if (request.url.endsWith('/commit')) {
        void request
          .clone()
          .json()
          .then((body) => commitBodies.push(body as ImportMappingDto));
      }
    });
    renderWithProviders(<ImportWizard sessionId={MOCK_SESSION_KYIV_ID} onFinished={onFinished} />);

    // Upload
    expect(next()).toBeDisabled();
    await user.upload(byQa(ImportsTestId.FileInput), xlsxFile());
    expect(byQa(ImportsTestId.FileName)).toHaveTextContent('participants.xlsx');
    await user.click(next());

    // Sheet & header row
    await waitFor(() => expect(byQa(ImportsTestId.SheetSelect)).toBeInTheDocument());
    expect(byQa(ImportsTestId.SheetSelect)).toHaveValue('Учасники');
    expect(within(byQa(ImportsTestId.SamplePreview)).getByText('Гнатюк')).toBeInTheDocument();
    await user.click(next());

    // Mapping: suggested mapping is prefilled; the unmapped "Телефон" column is offered as an attribute.
    expect(byQa(`${ImportsTestId.MappingField}-${ParticipantField.LastName}`)).toHaveValue(MockImportHeader.LastName);
    expect(byQa(`${ImportsTestId.MappingField}-${ParticipantField.FirstName}`)).toHaveValue(MockImportHeader.FirstName);
    expect(byQa(`${ImportsTestId.AttributeToggle}-${MockImportHeader.Profile}`)).toBeChecked();
    expect(byQa(`${ImportsTestId.AttributeToggle}-${MockImportHeader.Phone}`)).not.toBeChecked();

    await user.selectOptions(byQa(`${ImportsTestId.MappingField}-${ParticipantField.FirstName}`), '');
    await user.click(next());
    expect(screen.getByText(texts.mapping.namesRequired)).toBeInTheDocument();
    await user.selectOptions(byQa(`${ImportsTestId.MappingField}-${ParticipantField.FirstName}`), MockImportHeader.FirstName);
    await user.click(byQa(`${ImportsTestId.AttributeToggle}-${MockImportHeader.Phone}`));
    await user.click(next());

    // Validation report
    const summary = await waitFor(() => byQa(ImportsTestId.ValidationSummary) ?? Promise.reject(new Error('no summary')));
    expect(within(summary).getByText(fill(texts.validation.validRows, { count: MOCK_IMPORT_VALID_ROWS }))).toBeInTheDocument();
    expect(within(summary).getByText(new RegExp(texts.validation.codes.BooleanUnparseable))).toBeInTheDocument();
    expect(within(byQa(ImportsTestId.ValidationErrors)).getAllByText(texts.validation.codes.RequiredMissing).length).toBeGreaterThan(0);
    await user.click(next());

    // Mode
    await user.click(screen.getByLabelText(texts.mode.modes.Replace));
    await user.click(screen.getByLabelText(texts.mode.dedupe.None));
    await user.click(next());

    // Commit summary: errors remain, so commit is blocked until "skip invalid" is chosen.
    await waitFor(() => expect(byQa(ImportsTestId.CommitSummary)).toBeInTheDocument());
    expect(screen.getByText(texts.commit.blocked)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: texts.wizard.commitAction })).toBeDisabled();

    await user.click(screen.getByRole('button', { name: common.actions.back }));
    await user.click(byQa(ImportsTestId.SkipInvalid));
    await user.click(next());
    await waitFor(() => expect(screen.getByRole('button', { name: texts.wizard.commitAction })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: texts.wizard.commitAction }));

    // Result
    await waitFor(() => expect(byQa(ImportsTestId.Result)).toBeInTheDocument());
    expect(screen.getByText(fill(texts.commit.inserted, { count: MOCK_IMPORT_VALID_ROWS }))).toBeInTheDocument();
    expect(mockParticipantsCount(MOCK_SESSION_KYIV_ID)).toBe(MOCK_IMPORT_VALID_ROWS);
    expect(commitBodies[0]).toMatchObject({
      sheet: 'Учасники',
      headerRow: 1,
      mode: ImportMode.Replace,
      dedupeBy: DedupeStrategy.None,
      skipInvalid: true,
      mapping: { attributes: { profil: MockImportHeader.Profile, [toAttributeKey(MockImportHeader.Phone)]: MockImportHeader.Phone } },
    });

    await user.click(byQa(ImportsTestId.FinishButton));
    expect(onFinished).toHaveBeenCalled();
    server.events.removeAllListeners();
  });

  it('re-reads headers when the header row changes', async () => {
    const user = userEvent.setup();
    const previewBodies: unknown[] = [];
    server.events.on('request:start', ({ request }) => {
      if (request.url.endsWith('/preview')) {
        void request
          .clone()
          .json()
          .then((body) => previewBodies.push(body));
      }
    });
    renderWithProviders(<ImportWizard sessionId={MOCK_SESSION_KYIV_ID} onFinished={jest.fn()} />);

    await user.upload(byQa(ImportsTestId.FileInput), xlsxFile());
    await user.click(next());
    await waitFor(() => expect(byQa(ImportsTestId.HeaderRowField)).toBeInTheDocument());
    await user.clear(byQa(ImportsTestId.HeaderRowField));
    await user.type(byQa(ImportsTestId.HeaderRowField), '2');
    await user.click(next());

    await waitFor(() => expect(byQa(`${ImportsTestId.MappingField}-${ParticipantField.LastName}`)).toBeInTheDocument());
    expect(previewBodies).toEqual([{ sheet: 'Учасники', headerRow: 2 }]);
    server.events.removeAllListeners();
  });

  it('reports an upload error from the API', async () => {
    server.use(http.post(mockUrl(apiRoutes.imports.upload(':sessionId')), () => HttpResponse.json({ code: 'ImportFileInvalid', status: 400 }, { status: 400 })));
    const user = userEvent.setup();
    renderWithProviders(<ImportWizard sessionId={MOCK_SESSION_KYIV_ID} onFinished={jest.fn()} />);

    await user.upload(byQa(ImportsTestId.FileInput), xlsxFile());
    await user.click(next());

    expect(await screen.findByText(common.errors.ImportFileInvalid)).toBeInTheDocument();
  });

  it('checks file type and size on the client', () => {
    expect(checkFile(new File(['x'], 'list.pdf'))).toBe(FileError.WrongType);
    expect(checkFile(new File(['x'], 'list.CSV'))).toBeNull();
    const big = new File(['x'], 'big.xlsx');
    Object.defineProperty(big, 'size', { value: 21 * 1024 * 1024 });
    expect(checkFile(big)).toBe(FileError.TooLarge);
  });
});
