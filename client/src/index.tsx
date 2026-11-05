import { createRoot } from 'react-dom/client';
import App from './components/App';
import { appConfig } from './constants/config';
import { MSW_UNHANDLED_REQUEST_STRATEGY } from './constants/mocks';
import './i18n';

const ROOT_ELEMENT_ID = 'root';

const start = async () => {
  if (appConfig.useMocks) {
    const { worker } = await import('./mocks/browser');
    await worker.start({ onUnhandledRequest: MSW_UNHANDLED_REQUEST_STRATEGY });
  }

  const container = document.getElementById(ROOT_ELEMENT_ID);
  if (!container) {
    throw new Error(`Element #${ROOT_ELEMENT_ID} not found`);
  }

  createRoot(container).render(<App />);
};

void start();
