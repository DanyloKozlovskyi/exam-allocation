// Test-only render helper shared by the WS-E feature tests (providers as in components/App).
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { SnackbarProvider } from 'notistack';
import type { ReactElement } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';

interface Options {
  path?: string;
  route?: string;
}

export const renderWithProviders = (ui: ReactElement, { path, route }: Options = {}) => {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false }, mutations: { retry: false } } });
  const content = path ? (
    <Routes>
      <Route path={path} element={ui} />
    </Routes>
  ) : (
    ui
  );
  return {
    queryClient,
    ...render(
      <QueryClientProvider client={queryClient}>
        <SnackbarProvider>
          <MemoryRouter initialEntries={route ? [route] : undefined}>{content}</MemoryRouter>
        </SnackbarProvider>
      </QueryClientProvider>
    ),
  };
};
