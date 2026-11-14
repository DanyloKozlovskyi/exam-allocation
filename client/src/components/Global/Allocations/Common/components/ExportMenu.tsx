import DownloadIcon from '@mui/icons-material/Download';
import { Button, Menu, MenuItem } from '@mui/material';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ExportFormat } from '../../../../../api/dtos';
import { AllocationsTestId } from '../../../../../enums/Allocations';
import { I18nNamespace } from '../../../../../i18n';
import { EXPORT_FORMATS } from '../constants';
import { buildExportUrl } from '../helpers';

interface Props {
  allocationId: string;
  /** When set, adds a PDF for this room only. */
  roomId?: string;
}

const ExportMenu = ({ allocationId, roomId }: Props) => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const close = () => setAnchor(null);

  return (
    <>
      <Button variant="outlined" startIcon={<DownloadIcon />} onClick={(event) => setAnchor(event.currentTarget)} data-qa={AllocationsTestId.ExportButton} aria-haspopup="menu">
        {t('actions.export')}
      </Button>
      <Menu anchorEl={anchor} open={Boolean(anchor)} onClose={close}>
        {EXPORT_FORMATS.map((format) => (
          <MenuItem key={format} component="a" href={buildExportUrl(allocationId, format)} download onClick={close} data-qa={AllocationsTestId.ExportMenuItem} data-format={format}>
            {t(`export.${format}`)}
          </MenuItem>
        ))}
        {roomId && (
          <MenuItem
            component="a"
            href={buildExportUrl(allocationId, ExportFormat.Pdf, roomId)}
            download
            onClick={close}
            data-qa={AllocationsTestId.ExportMenuItem}
            data-format={ExportFormat.Pdf}>
            {t('export.roomPdf')}
          </MenuItem>
        )}
      </Menu>
    </>
  );
};

export default ExportMenu;
