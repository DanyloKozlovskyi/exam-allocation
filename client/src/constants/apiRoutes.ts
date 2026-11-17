// Mirrors api/src/Api/Constants/ApiRoutes.cs — the only place in the client that knows endpoint paths.

const SESSIONS = 'sessions';
const CENTERS = 'centers';
const ROOMS = 'rooms';
const PARTICIPANTS = 'participants';
const RULES = 'rules';
const PINS = 'pins';
const IMPORTS = 'imports';
const SOLVE_RUNS = 'solve-runs';
const ALLOCATIONS = 'allocations';
const CREATE = 'create';
const EDIT = 'edit';

const session = (sessionId: string) => `${SESSIONS}/${sessionId}`;
const allocation = (allocationId: string) => `${ALLOCATIONS}/${allocationId}`;
const solveRun = (runId: string) => `${SOLVE_RUNS}/${runId}`;

/** Query parameter names (mirror the `[FromQuery]` names of the API controllers). */
export enum ApiQueryParam {
  Format = 'format',
  RoomId = 'roomId',
  Page = 'page',
  PageSize = 'pageSize',
  Search = 'search',
}

export const apiRoutes = {
  health: () => '/health',
  sessions: {
    list: () => SESSIONS,
    byId: (sessionId: string) => session(sessionId),
    totals: (sessionId: string) => `${session(sessionId)}/totals`,
    create: () => `${SESSIONS}/${CREATE}`,
    edit: () => `${SESSIONS}/${EDIT}`,
  },
  centers: {
    list: (sessionId: string) => `${session(sessionId)}/${CENTERS}`,
    byId: (sessionId: string, centerId: string) => `${session(sessionId)}/${CENTERS}/${centerId}`,
    create: (sessionId: string) => `${session(sessionId)}/${CENTERS}/${CREATE}`,
    edit: (sessionId: string) => `${session(sessionId)}/${CENTERS}/${EDIT}`,
  },
  rooms: {
    list: (centerId: string) => `${CENTERS}/${centerId}/${ROOMS}`,
    byId: (centerId: string, roomId: string) => `${CENTERS}/${centerId}/${ROOMS}/${roomId}`,
    create: (centerId: string) => `${CENTERS}/${centerId}/${ROOMS}/${CREATE}`,
    createBulk: (centerId: string) => `${CENTERS}/${centerId}/${ROOMS}/create-bulk`,
    edit: (centerId: string) => `${CENTERS}/${centerId}/${ROOMS}/${EDIT}`,
    previewLayout: () => `${ROOMS}/preview-layout`,
  },
  participants: {
    list: (sessionId: string) => `${session(sessionId)}/${PARTICIPANTS}`,
    byId: (sessionId: string, participantId: string) => `${session(sessionId)}/${PARTICIPANTS}/${participantId}`,
    create: (sessionId: string) => `${session(sessionId)}/${PARTICIPANTS}/${CREATE}`,
    edit: (sessionId: string) => `${session(sessionId)}/${PARTICIPANTS}/${EDIT}`,
    deleteAll: (sessionId: string) => `${session(sessionId)}/${PARTICIPANTS}/delete-all`,
    attributes: (sessionId: string) => `${session(sessionId)}/attributes`,
  },
  rules: {
    list: (sessionId: string) => `${session(sessionId)}/${RULES}`,
    byId: (sessionId: string, ruleId: string) => `${session(sessionId)}/${RULES}/${ruleId}`,
    create: (sessionId: string) => `${session(sessionId)}/${RULES}/${CREATE}`,
    edit: (sessionId: string) => `${session(sessionId)}/${RULES}/${EDIT}`,
    ruleTypes: () => 'rule-types',
  },
  pins: {
    list: (sessionId: string) => `${session(sessionId)}/${PINS}`,
    create: (sessionId: string) => `${session(sessionId)}/${PINS}/${CREATE}`,
    byParticipant: (sessionId: string, participantId: string) => `${session(sessionId)}/${PINS}/${participantId}`,
    deleteAll: (sessionId: string) => `${session(sessionId)}/${PINS}/delete-all`,
    pinAll: (allocationId: string) => `${allocation(allocationId)}/pin-all`,
  },
  imports: {
    upload: (sessionId: string) => `${session(sessionId)}/${IMPORTS}/upload`,
    preview: (importId: string) => `${IMPORTS}/${importId}/preview`,
    validate: (importId: string) => `${IMPORTS}/${importId}/validate`,
    commit: (importId: string) => `${IMPORTS}/${importId}/commit`,
    template: () => `${IMPORTS}/template`,
  },
  solveRuns: {
    list: (sessionId: string) => `${session(sessionId)}/${SOLVE_RUNS}`,
    create: (sessionId: string) => `${session(sessionId)}/${SOLVE_RUNS}/${CREATE}`,
    byId: (runId: string) => solveRun(runId),
    cancel: (runId: string) => `${solveRun(runId)}/cancel`,
    precheck: (sessionId: string) => `${session(sessionId)}/precheck`,
  },
  allocations: {
    byRun: (runId: string) => `${solveRun(runId)}/${ALLOCATIONS}`,
    byId: (allocationId: string) => allocation(allocationId),
    roomSeating: (allocationId: string, roomId: string) => `${allocation(allocationId)}/${ROOMS}/${roomId}`,
    participants: (allocationId: string) => `${allocation(allocationId)}/${PARTICIPANTS}`,
    validateMoves: (allocationId: string) => `${allocation(allocationId)}/validate-moves`,
    applyMoves: (allocationId: string) => `${allocation(allocationId)}/apply-moves`,
  },
  exports: {
    export: (allocationId: string) => `${allocation(allocationId)}/export`,
  },
};
