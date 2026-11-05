import { render, screen } from '@testing-library/react';
import App from '.';
import { resources, Language } from '../../i18n';

describe('App', () => {
  it('renders the shell with title and navigation', async () => {
    render(<App />);

    expect(await screen.findByText(resources[Language.Ukrainian].common.appTitle)).toBeInTheDocument();
    expect(await screen.findByText(resources[Language.Ukrainian].common.views.sessionsList)).toBeInTheDocument();
  });
});
