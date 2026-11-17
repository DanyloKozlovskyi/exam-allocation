import { AccessibleFilter } from '../../../../enums/Participants';
import { BuiltInAttributeKey } from '../../../../enums/Rules';
import { DiagnosticArgument, DiagnosticCode, type DiagnosticDto } from '../../../../api/dtos';
import { routes } from '../../../../constants/routes';
import { DiagnosticLinkTarget } from '../../../../enums/SolveRuns';

/** Separator of list-valued diagnostic arguments (`RuleIds`, `CenterId`); mirrors `DiagnosticFactory.ListSeparator`. */
export const DIAGNOSTIC_LIST_SEPARATOR = ',';

/**
 * Where to fix each diagnostic (docs/04 §7), in display order. Exhaustive on purpose: a new
 * `DiagnosticCode` fails typecheck until it is mapped. An empty list means "suggestion only".
 * The suggestion text itself is `diagnostics.suggestion.<code>` (solveRuns.json).
 */
export const DIAGNOSTIC_LINK_TARGETS: Record<DiagnosticCode, DiagnosticLinkTarget[]> = {
  [DiagnosticCode.CapacityTotalExceeded]: [DiagnosticLinkTarget.CentersList, DiagnosticLinkTarget.Participants],
  [DiagnosticCode.AccessibleCapacityExceeded]: [DiagnosticLinkTarget.CentersList, DiagnosticLinkTarget.ParticipantsAccessible, DiagnosticLinkTarget.Rule],
  [DiagnosticCode.SeparationImpossible]: [DiagnosticLinkTarget.Rule, DiagnosticLinkTarget.ParticipantsByAttribute],
  [DiagnosticCode.NoAdjacencyImpossible]: [DiagnosticLinkTarget.Rule, DiagnosticLinkTarget.ParticipantsByAttribute, DiagnosticLinkTarget.CentersList],
  [DiagnosticCode.PinInvalidSeat]: [DiagnosticLinkTarget.Pins, DiagnosticLinkTarget.RoomEdit],
  [DiagnosticCode.PinDuplicateSeat]: [DiagnosticLinkTarget.Pins, DiagnosticLinkTarget.RoomEdit],
  [DiagnosticCode.PinRoomOverfull]: [DiagnosticLinkTarget.Pins, DiagnosticLinkTarget.RoomEdit],
  [DiagnosticCode.PinRuleConflict]: [DiagnosticLinkTarget.Rule, DiagnosticLinkTarget.Pins, DiagnosticLinkTarget.RoomEdit, DiagnosticLinkTarget.ParticipantsByAttribute],
  [DiagnosticCode.AttributeMissing]: [DiagnosticLinkTarget.ParticipantsByAttribute, DiagnosticLinkTarget.ParticipantsImport],
  [DiagnosticCode.NoActiveRooms]: [DiagnosticLinkTarget.CentersList],
  [DiagnosticCode.ModelTooLarge]: [DiagnosticLinkTarget.RulesList],
  [DiagnosticCode.SeatingInfeasible]: [DiagnosticLinkTarget.RoomEdit, DiagnosticLinkTarget.ParticipantsByAttribute, DiagnosticLinkTarget.RulesList],
  [DiagnosticCode.ConflictCore]: [DiagnosticLinkTarget.Rule, DiagnosticLinkTarget.CenterRooms],
  [DiagnosticCode.AlternativesLimited]: [],
  [DiagnosticCode.InternalValidationFailed]: [],
};

/** Interpolation values of a link label (`diagnostics.link.<target>`). */
export enum DiagnosticLinkLabelArg {
  Name = 'name',
  Attribute = 'attribute',
  Value = 'value',
}

interface LinkSpec {
  to: string;
  /** Raw ids/keys for the label; the panel turns them into display names. */
  labelArgs: Partial<Record<DiagnosticLinkLabelArg, string>>;
}

export interface DiagnosticLink extends LinkSpec {
  target: DiagnosticLinkTarget;
}

export interface RoomRef {
  centerId: string;
  name?: string;
}

export interface DiagnosticLinkContext {
  sessionId: string;
  /** roomId → its center (rooms of loaded centers); a room link needs the center for its route. */
  rooms: ReadonlyMap<string, RoomRef>;
}

export const splitList = (value: string | undefined) =>
  (value ?? '')
    .split(DIAGNOSTIC_LIST_SEPARATOR)
    .map((item) => item.trim())
    .filter(Boolean);

const argument = (diagnostic: DiagnosticDto, key: DiagnosticArgument) => diagnostic.arguments?.[key] || undefined;

/** `ruleId` plus every id of `RuleIds`, without duplicates. */
export const diagnosticRuleIds = (diagnostic: DiagnosticDto) => [
  ...new Set([...(diagnostic.ruleId ? [diagnostic.ruleId] : []), ...splitList(argument(diagnostic, DiagnosticArgument.RuleIds))]),
];

/** Room ids referenced by a diagnostic (`RoomId`). */
export const diagnosticRoomIds = (diagnostic: DiagnosticDto) => splitList(argument(diagnostic, DiagnosticArgument.RoomId));

interface LinkBuilder {
  (diagnostic: DiagnosticDto, context: DiagnosticLinkContext): LinkSpec[];
}

// Participants links preset the list filters via the URL (group, school, accessible); custom attributes
// have no API filter, so their links open the unfiltered list.
const LINK_BUILDERS: Record<DiagnosticLinkTarget, LinkBuilder> = {
  [DiagnosticLinkTarget.Rule]: (diagnostic, { sessionId }) =>
    diagnosticRuleIds(diagnostic).map((ruleId) => ({ to: routes.rules.edit(sessionId, ruleId), labelArgs: { [DiagnosticLinkLabelArg.Name]: ruleId } })),
  [DiagnosticLinkTarget.RulesList]: (_diagnostic, { sessionId }) => [{ to: routes.rules.list(sessionId), labelArgs: {} }],
  [DiagnosticLinkTarget.CenterRooms]: (diagnostic, { sessionId }) =>
    splitList(argument(diagnostic, DiagnosticArgument.CenterId)).map((centerId) => ({
      to: routes.rooms.list(sessionId, centerId),
      labelArgs: { [DiagnosticLinkLabelArg.Name]: centerId },
    })),
  [DiagnosticLinkTarget.CentersList]: (_diagnostic, { sessionId }) => [{ to: routes.centers.list(sessionId), labelArgs: {} }],
  [DiagnosticLinkTarget.RoomEdit]: (diagnostic, { sessionId, rooms }) =>
    diagnosticRoomIds(diagnostic).flatMap((roomId) => {
      const room = rooms.get(roomId);
      // Center unknown (rooms not loaded, or room deleted): fall back to the centers page.
      return room ? [{ to: routes.rooms.edit(sessionId, room.centerId, roomId), labelArgs: { [DiagnosticLinkLabelArg.Name]: room.name ?? roomId } }] : [];
    }),
  [DiagnosticLinkTarget.Participants]: (_diagnostic, { sessionId }) => [{ to: routes.participants.list(sessionId), labelArgs: {} }],
  [DiagnosticLinkTarget.ParticipantsByAttribute]: (diagnostic, { sessionId }) => {
    const attribute = argument(diagnostic, DiagnosticArgument.AttributeKey);
    if (!attribute) return [];
    const value = argument(diagnostic, DiagnosticArgument.Value);
    // Built-in attributes are list filters; custom attributes have no API filter yet (plain list).
    const filter = !value ? {} : attribute === BuiltInAttributeKey.Group ? { group: value } : attribute === BuiltInAttributeKey.School ? { school: value } : {};
    return [
      {
        to: routes.participants.list(sessionId, filter),
        labelArgs: { [DiagnosticLinkLabelArg.Attribute]: attribute, ...(value ? { [DiagnosticLinkLabelArg.Value]: value } : {}) },
      },
    ];
  },
  [DiagnosticLinkTarget.ParticipantsAccessible]: (_diagnostic, { sessionId }) => [{ to: routes.participants.list(sessionId, { accessible: AccessibleFilter.Yes }), labelArgs: {} }],
  [DiagnosticLinkTarget.ParticipantsImport]: (_diagnostic, { sessionId }) => [{ to: routes.participants.import(sessionId), labelArgs: {} }],
  [DiagnosticLinkTarget.Pins]: (_diagnostic, { sessionId }) => [{ to: routes.pins.list(sessionId), labelArgs: {} }],
};

/** Deep links of one diagnostic, in table order; a room whose center is unknown degrades to the centers page. */
export const buildDiagnosticLinks = (diagnostic: DiagnosticDto, context: DiagnosticLinkContext): DiagnosticLink[] => {
  if (!diagnostic.code) return [];
  const links = DIAGNOSTIC_LINK_TARGETS[diagnostic.code].flatMap((target) => {
    const built = LINK_BUILDERS[target](diagnostic, context).map((link) => ({ ...link, target }));
    if (target === DiagnosticLinkTarget.RoomEdit && built.length < diagnosticRoomIds(diagnostic).length) {
      built.push(...LINK_BUILDERS[DiagnosticLinkTarget.CentersList](diagnostic, context).map((link) => ({ ...link, target: DiagnosticLinkTarget.CentersList })));
    }
    return built;
  });
  const seen = new Set<string>();
  // Same page twice (e.g. two participants links) is shown once, with the first, most specific label.
  return links.filter((link) => !seen.has(link.to) && Boolean(seen.add(link.to)));
};
