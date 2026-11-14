import { HubConnectionBuilder, HubConnectionState, type HubConnection } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import type { SolveProgressDto, SolveRunDto } from '../api/dtos';
import { useSolveRunQuery } from '../components/Global/Queries/solveRuns';
import { isRunActive, OBJECTIVE_HISTORY_LIMIT } from '../components/Global/SolveRuns/Common/constants';
import { appConfig } from '../constants/config';
import { HubEvent, HubMethod, HubRoute } from '../constants/hub';
import { QueryKey, queryKeys } from '../constants/queryKeys';
import { SolveProgressTransport } from '../enums/SolveRuns';

export interface ObjectivePoint {
  objective: number;
  bestBound: number | null;
}

export interface SolveProgressState {
  run: SolveRunDto | undefined;
  progress: SolveProgressDto | null;
  history: ObjectivePoint[];
  transport: SolveProgressTransport;
  isActive: boolean;
  isLoading: boolean;
}

interface Options {
  /** Defaults to polling on mocks (no hub there) and SignalR otherwise. */
  transport?: SolveProgressTransport;
}

export const hubUrl = () => `${appConfig.apiUrl}${HubRoute.SolveProgress}`;

/**
 * Live progress of a solve run: SignalR `SolveProgressHub` (SubscribeToRun → ProgressUpdated / RunCompleted),
 * falling back to polling `solve-runs/{id}` every SOLVE_POLL_INTERVAL_MS when the hub is unavailable.
 */
export const useSolveProgress = (runId: string, { transport: preferred }: Options = {}): SolveProgressState => {
  const queryClient = useQueryClient();
  const initial = preferred ?? (appConfig.useMocks ? SolveProgressTransport.Polling : SolveProgressTransport.Hub);
  const [transport, setTransport] = useState(initial);
  const [progress, setProgress] = useState<SolveProgressDto | null>(null);
  const [history, setHistory] = useState<ObjectivePoint[]>([]);
  const { data: run, isLoading } = useSolveRunQuery(runId, { poll: transport === SolveProgressTransport.Polling });

  useEffect(() => {
    if (initial !== SolveProgressTransport.Hub || !runId) {
      return undefined;
    }

    let disposed = false;
    const connection: HubConnection = new HubConnectionBuilder().withUrl(hubUrl()).withAutomaticReconnect().build();
    const fallBack = () => {
      if (!disposed) {
        setTransport(SolveProgressTransport.Polling);
      }
    };

    connection.on(HubEvent.ProgressUpdated, (update: SolveProgressDto) => {
      if (disposed || update.runId !== runId) {
        return;
      }
      setProgress(update);
      if (update.objective !== null && update.objective !== undefined) {
        const point = { objective: update.objective, bestBound: update.bestBound ?? null };
        setHistory((current) => [...current, point].slice(-OBJECTIVE_HISTORY_LIMIT));
      }
    });
    connection.on(HubEvent.RunCompleted, (completed: SolveRunDto) => {
      if (disposed || completed.id !== runId) {
        return;
      }
      queryClient.setQueryData(queryKeys.solveRuns.detail(runId), completed);
      void queryClient.invalidateQueries({ queryKey: [QueryKey.SolveRuns, completed.sessionId] });
    });
    connection.onclose(fallBack);

    connection
      .start()
      .then(() => (disposed ? undefined : connection.invoke(HubMethod.SubscribeToRun, runId)))
      .catch(fallBack);

    return () => {
      disposed = true;
      const stop = () => connection.stop().catch(() => undefined);
      if (connection.state === HubConnectionState.Connected) {
        void connection
          .invoke(HubMethod.UnsubscribeFromRun, runId)
          .catch(() => undefined)
          .finally(stop);
      } else {
        void stop();
      }
    };
  }, [runId, initial, queryClient]);

  return { run, progress, history, transport, isActive: isRunActive(run?.status), isLoading };
};
