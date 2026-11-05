// Mirrors api/src/Api/Constants/HubRoutes.cs.
export enum HubRoute {
  SolveProgress = '/hubs/solve-progress',
}

export enum HubMethod {
  SubscribeToRun = 'SubscribeToRun',
  UnsubscribeFromRun = 'UnsubscribeFromRun',
}

export enum HubEvent {
  ProgressUpdated = 'ProgressUpdated',
  RunCompleted = 'RunCompleted',
}
