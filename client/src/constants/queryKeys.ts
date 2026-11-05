export enum QueryKey {
  Sessions = 'sessions',
  SessionTotals = 'session-totals',
  Centers = 'centers',
  Rooms = 'rooms',
  Participants = 'participants',
  Attributes = 'attributes',
  Rules = 'rules',
  RuleTypes = 'rule-types',
  Pins = 'pins',
  SolveRuns = 'solve-runs',
  Allocations = 'allocations',
  AllocationRoom = 'allocation-room',
  AllocationParticipants = 'allocation-participants',
}

export const queryKeys = {
  sessions: {
    list: () => [QueryKey.Sessions] as const,
    detail: (sessionId: string) => [QueryKey.Sessions, sessionId] as const,
    totals: (sessionId: string) => [QueryKey.SessionTotals, sessionId] as const,
  },
  centers: {
    list: (sessionId: string) => [QueryKey.Centers, sessionId] as const,
    detail: (sessionId: string, centerId: string) => [QueryKey.Centers, sessionId, centerId] as const,
  },
  rooms: {
    list: (centerId: string) => [QueryKey.Rooms, centerId] as const,
    detail: (centerId: string, roomId: string) => [QueryKey.Rooms, centerId, roomId] as const,
  },
  participants: {
    page: (sessionId: string, params: object) => [QueryKey.Participants, sessionId, params] as const,
    detail: (sessionId: string, participantId: string) => [QueryKey.Participants, sessionId, participantId] as const,
    attributes: (sessionId: string) => [QueryKey.Attributes, sessionId] as const,
  },
  rules: {
    list: (sessionId: string) => [QueryKey.Rules, sessionId] as const,
    detail: (sessionId: string, ruleId: string) => [QueryKey.Rules, sessionId, ruleId] as const,
    types: () => [QueryKey.RuleTypes] as const,
  },
  pins: {
    list: (sessionId: string) => [QueryKey.Pins, sessionId] as const,
  },
  solveRuns: {
    list: (sessionId: string) => [QueryKey.SolveRuns, sessionId] as const,
    detail: (runId: string) => [QueryKey.SolveRuns, runId] as const,
  },
  allocations: {
    byRun: (runId: string) => [QueryKey.Allocations, QueryKey.SolveRuns, runId] as const,
    detail: (allocationId: string) => [QueryKey.Allocations, allocationId] as const,
    room: (allocationId: string, roomId: string) => [QueryKey.AllocationRoom, allocationId, roomId] as const,
    participants: (allocationId: string, params: object) => [QueryKey.AllocationParticipants, allocationId, params] as const,
  },
};
