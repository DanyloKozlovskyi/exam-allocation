import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import common from './uk/common.json';
import sessions from './uk/sessions.json';
import centers from './uk/centers.json';
import rooms from './uk/rooms.json';
import participants from './uk/participants.json';
import imports from './uk/imports.json';
import rules from './uk/rules.json';
import solveRuns from './uk/solveRuns.json';
import allocations from './uk/allocations.json';
import pins from './uk/pins.json';

export enum I18nNamespace {
  Common = 'common',
  Sessions = 'sessions',
  Centers = 'centers',
  Rooms = 'rooms',
  Participants = 'participants',
  Imports = 'imports',
  Rules = 'rules',
  SolveRuns = 'solveRuns',
  Allocations = 'allocations',
  Pins = 'pins',
}

export enum Language {
  Ukrainian = 'uk',
}

export const resources = {
  [Language.Ukrainian]: { common, sessions, centers, rooms, participants, imports, rules, solveRuns, allocations, pins },
} as const;

void i18n.use(initReactI18next).init({
  resources,
  lng: Language.Ukrainian,
  fallbackLng: Language.Ukrainian,
  defaultNS: I18nNamespace.Common,
  ns: Object.values(I18nNamespace),
  interpolation: { escapeValue: false },
});

export default i18n;
