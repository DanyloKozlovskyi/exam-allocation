import { http, HttpResponse, type HttpHandler } from 'msw';
import { ErrorCode, type PinDto } from '../../api/dtos';
import { apiRoutes } from '../../constants/apiRoutes';
import { HttpStatus, MockPathParam, mockUrl, pathParam, problem, registerMockStore, touchMockSessionInput } from './sessions';

/** Pins per session (filled by `pin-all` in the allocations mocks); keyed by participant like the API. */
let pinsBySession = new Map<string, Map<string, PinDto>>();

const sessionPins = (sessionId: string) => {
  let pins = pinsBySession.get(sessionId);
  if (!pins) {
    pins = new Map();
    pinsBySession.set(sessionId, pins);
  }
  return pins;
};

/** Stores pins created by `pin-all` (an existing pin of the same participant is replaced). */
export const storeMockPins = (sessionId: string, pins: PinDto[]) => {
  const store = sessionPins(sessionId);
  pins.forEach((pin) => pin.participantId && store.set(pin.participantId, pin));
};

export const getMockPins = (sessionId: string) => [...sessionPins(sessionId).values()];

registerMockStore({
  totals: () => ({}),
  removeSession: (sessionId) => pinsBySession.delete(sessionId),
  reset: () => {
    pinsBySession = new Map();
  },
});

const sessionIdParam = pathParam(MockPathParam.SessionId);
const participantIdParam = pathParam(MockPathParam.ParticipantId);

export const pinsHandlers: HttpHandler[] = [
  http.get(mockUrl(apiRoutes.pins.list(sessionIdParam)), ({ params }) => HttpResponse.json(getMockPins(String(params[MockPathParam.SessionId])))),
  http.post(mockUrl(apiRoutes.pins.deleteAll(sessionIdParam)), ({ params }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    sessionPins(sessionId).clear();
    touchMockSessionInput(sessionId);
    return new HttpResponse(null, { status: HttpStatus.NoContent });
  }),
  http.delete(mockUrl(apiRoutes.pins.byParticipant(sessionIdParam, participantIdParam)), ({ params }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    if (!sessionPins(sessionId).delete(String(params[MockPathParam.ParticipantId]))) {
      return problem(HttpStatus.NotFound, ErrorCode.PinNotFound);
    }
    touchMockSessionInput(sessionId);
    return new HttpResponse(null, { status: HttpStatus.NoContent });
  }),
];
