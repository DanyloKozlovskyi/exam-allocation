// Test-only helpers shared by every feature test (providers as in components/App).
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { SnackbarProvider } from 'notistack';
import { createElement, type ReactElement } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import LocationDisplay from './LocationDisplay';

export { LOCATION_TEST_ID } from './LocationDisplay';

const ANY_PATH = '*';

interface Options {
  /** Initial URL. */
  route?: string;
  /** Route pattern the element is mounted at (a RoutePath); without it the element is rendered directly. */
  path?: string;
}

/** Renders a feature component with the app providers and a router, plus the current location (LOCATION_TEST_ID). */
export const renderWithProviders = (ui: ReactElement, { route, path }: Options = {}) => {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false }, mutations: { retry: false } } });
  const content = path ? createElement(Routes, null, createElement(Route, { path, element: ui }), createElement(Route, { path: ANY_PATH, element: null })) : ui;
  const tree = createElement(
    QueryClientProvider,
    { client: queryClient },
    createElement(SnackbarProvider, null, createElement(MemoryRouter, { initialEntries: route ? [route] : undefined }, content, createElement(LocationDisplay)))
  );
  return { queryClient, ...render(tree) };
};

export const byQa = (container: HTMLElement, qa: string) => container.querySelector(`[data-qa="${qa}"]`);
export const allByQa = (container: HTMLElement, qa: string) => Array.from(container.querySelectorAll(`[data-qa="${qa}"]`));
