import DownloadIcon from '@mui/icons-material/Download';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import { Alert, Button, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { ImportFileExtension, ImportLimit, ImportsTestId } from '../../../../../enums/Imports';
import { I18nNamespace } from '../../../../../i18n';
import { checkFile, ImportReducerAction, type ImportAction, type ImportState } from '..';

const ACCEPT = Object.values(ImportFileExtension).join(',');

interface Props {
  state: ImportState;
  dispatch: React.Dispatch<ImportAction>;
  onDownloadTemplate: () => void;
}

const StepUpload = ({ state, dispatch, onDownloadTemplate }: Props) => {
  const { t } = useTranslation(I18nNamespace.Imports);

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null;
    dispatch({ type: ImportReducerAction.File, payload: { file, fileError: file ? checkFile(file) : null } });
  };

  return (
    <Stack spacing={2}>
      <Typography color="text.secondary">{t('upload.hint', { max: ImportLimit.MaxFileSizeMb })}</Typography>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
        <Button variant="outlined" component="label" startIcon={<UploadFileIcon />}>
          {t('upload.chooseFile')}
          <input hidden type="file" accept={ACCEPT} onChange={handleChange} data-qa={ImportsTestId.FileInput} />
        </Button>
        <Typography data-qa={ImportsTestId.FileName}>{state.file?.name ?? t('upload.noFile')}</Typography>
      </Stack>
      {state.fileError && <Alert severity="error">{t(`upload.${state.fileError}`, { max: ImportLimit.MaxFileSizeMb })}</Alert>}
      {state.preview && state.file && <Alert severity="success">{t('upload.uploaded', { name: state.file.name })}</Alert>}
      <div>
        <Button startIcon={<DownloadIcon />} onClick={onDownloadTemplate} data-qa={ImportsTestId.TemplateButton}>
          {t('upload.template')}
        </Button>
      </div>
    </Stack>
  );
};

export default StepUpload;
