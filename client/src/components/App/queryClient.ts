import { QueryClient } from '@tanstack/react-query';
import { DEFAULT_STALE_TIME_MS } from '../../constants/queryParams';

export const createQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        refetchOnWindowFocus: false,
        staleTime: DEFAULT_STALE_TIME_MS,
      },
    },
  });
