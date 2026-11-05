import { generatePath } from 'react-router-dom';

export enum RoutePath {
  Home = '/',
  Sessions = '/sessions',
  SessionCreate = '/sessions/create',
  SessionView = '/sessions/:sessionId',
  SessionEdit = '/sessions/:sessionId/edit',
  Centers = '/sessions/:sessionId/centers',
  CenterCreate = '/sessions/:sessionId/centers/create',
  CenterEdit = '/sessions/:sessionId/centers/:centerId/edit',
  Rooms = '/sessions/:sessionId/centers/:centerId/rooms',
  RoomCreate = '/sessions/:sessionId/centers/:centerId/rooms/create',
  RoomEdit = '/sessions/:sessionId/centers/:centerId/rooms/:roomId/edit',
  Participants = '/sessions/:sessionId/participants',
  ParticipantsImport = '/sessions/:sessionId/participants/import',
  Rules = '/sessions/:sessionId/rules',
  RuleCreate = '/sessions/:sessionId/rules/create',
  RuleEdit = '/sessions/:sessionId/rules/:ruleId/edit',
  SolveRuns = '/sessions/:sessionId/solve-runs',
  SolveRunView = '/sessions/:sessionId/solve-runs/:runId',
  AllocationView = '/sessions/:sessionId/allocations/:allocationId',
  AllocationRoom = '/sessions/:sessionId/allocations/:allocationId/rooms/:roomId',
}

export enum RouteParam {
  SessionId = 'sessionId',
  CenterId = 'centerId',
  RoomId = 'roomId',
  RuleId = 'ruleId',
  RunId = 'runId',
  AllocationId = 'allocationId',
}

export const routes = {
  home: () => RoutePath.Home,
  sessions: {
    list: () => RoutePath.Sessions,
    create: () => RoutePath.SessionCreate,
    view: (sessionId: string) => generatePath(RoutePath.SessionView, { sessionId }),
    edit: (sessionId: string) => generatePath(RoutePath.SessionEdit, { sessionId }),
  },
  centers: {
    list: (sessionId: string) => generatePath(RoutePath.Centers, { sessionId }),
    create: (sessionId: string) => generatePath(RoutePath.CenterCreate, { sessionId }),
    edit: (sessionId: string, centerId: string) => generatePath(RoutePath.CenterEdit, { sessionId, centerId }),
  },
  rooms: {
    list: (sessionId: string, centerId: string) => generatePath(RoutePath.Rooms, { sessionId, centerId }),
    create: (sessionId: string, centerId: string) => generatePath(RoutePath.RoomCreate, { sessionId, centerId }),
    edit: (sessionId: string, centerId: string, roomId: string) => generatePath(RoutePath.RoomEdit, { sessionId, centerId, roomId }),
  },
  participants: {
    list: (sessionId: string) => generatePath(RoutePath.Participants, { sessionId }),
    import: (sessionId: string) => generatePath(RoutePath.ParticipantsImport, { sessionId }),
  },
  rules: {
    list: (sessionId: string) => generatePath(RoutePath.Rules, { sessionId }),
    create: (sessionId: string) => generatePath(RoutePath.RuleCreate, { sessionId }),
    edit: (sessionId: string, ruleId: string) => generatePath(RoutePath.RuleEdit, { sessionId, ruleId }),
  },
  solveRuns: {
    list: (sessionId: string) => generatePath(RoutePath.SolveRuns, { sessionId }),
    view: (sessionId: string, runId: string) => generatePath(RoutePath.SolveRunView, { sessionId, runId }),
  },
  allocations: {
    view: (sessionId: string, allocationId: string) => generatePath(RoutePath.AllocationView, { sessionId, allocationId }),
    room: (sessionId: string, allocationId: string, roomId: string) => generatePath(RoutePath.AllocationRoom, { sessionId, allocationId, roomId }),
  },
};
