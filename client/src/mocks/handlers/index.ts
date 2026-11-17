import { sessionsHandlers } from './sessions';
import { centersHandlers } from './centers';
import { roomsHandlers } from './rooms';
import { participantsHandlers } from './participants';
import { importsHandlers } from './imports';
import { rulesHandlers } from './rules';
import { solveRunsHandlers } from './solveRuns';
import { allocationsHandlers } from './allocations';
import { pinsHandlers } from './pins';

export const handlers = [
  ...sessionsHandlers,
  ...centersHandlers,
  ...roomsHandlers,
  ...participantsHandlers,
  ...importsHandlers,
  ...rulesHandlers,
  ...solveRunsHandlers,
  ...allocationsHandlers,
  ...pinsHandlers,
];
