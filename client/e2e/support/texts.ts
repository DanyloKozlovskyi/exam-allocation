// Ukrainian UI texts straight from the i18n resources (no copies of user-facing strings in the suite).
import allocations from '../../src/i18n/uk/allocations.json' with { type: 'json' };
import centers from '../../src/i18n/uk/centers.json' with { type: 'json' };
import common from '../../src/i18n/uk/common.json' with { type: 'json' };
import imports from '../../src/i18n/uk/imports.json' with { type: 'json' };
import rooms from '../../src/i18n/uk/rooms.json' with { type: 'json' };
import rules from '../../src/i18n/uk/rules.json' with { type: 'json' };
import sessions from '../../src/i18n/uk/sessions.json' with { type: 'json' };
import solveRuns from '../../src/i18n/uk/solveRuns.json' with { type: 'json' };

export const uk = { allocations, centers, common, imports, rooms, rules, sessions, solveRuns };

const PLACEHOLDER = /\{\{(\w+)\}\}/g;

/** i18next-style `{{name}}` interpolation of a resource string. */
export const format = (template: string, values: Record<string, string | number>) =>
  template.replace(PLACEHOLDER, (match, key: string) => (key in values ? String(values[key]) : match));

const REGEX_SPECIAL = /[.*+?^${}()|[\]\\]/g;

/** A resource string with `{{…}}` placeholders as a RegExp matching any value. */
export const pattern = (template: string) =>
  new RegExp(
    template
      .split(PLACEHOLDER)
      .map((part, index) => (index % 2 === 1 ? '.+?' : part.replace(REGEX_SPECIAL, '\\$&')))
      .join('')
  );
