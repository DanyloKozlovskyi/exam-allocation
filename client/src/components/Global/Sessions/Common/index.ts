import type { SessionDto } from '../../../../api/dtos';
import { FormFieldError, SessionConstraint } from '../../../../enums/Sessions';
import { emptyToNull, validateText } from './formHelpers';

export interface SessionState {
  id: string | null;
  version: number | null;
  name: string;
  /** ISO date (yyyy-mm-dd) or empty. */
  examDate: string;
  description: string;
}

export interface SessionValidationErrors {
  name?: FormFieldError;
  description?: FormFieldError;
}

export enum SessionReducerAction {
  Name = 'name',
  ExamDate = 'examDate',
  Description = 'description',
  Load = 'load',
}

export interface SessionAction {
  type: SessionReducerAction;
  payload: Partial<SessionState>;
}

export const initialSessionState: SessionState = { id: null, version: null, name: '', examDate: '', description: '' };

export const sessionReducer = (state: SessionState, action: SessionAction): SessionState => {
  switch (action.type) {
    case SessionReducerAction.Load:
      return { ...initialSessionState, ...action.payload };
    case SessionReducerAction.Name:
    case SessionReducerAction.ExamDate:
    case SessionReducerAction.Description:
      return { ...state, ...action.payload };
    default:
      return state;
  }
};

export const validateSession = (state: SessionState): SessionValidationErrors => ({
  name: validateText(state.name, SessionConstraint.NameMaxLength, true),
  description: validateText(state.description, SessionConstraint.DescriptionMaxLength, false),
});

const ISO_DATE_LENGTH = 10;

export const fromSessionDto = (dto: SessionDto): SessionState => ({
  id: dto.id ?? null,
  version: dto.version ?? null,
  name: dto.name ?? '',
  examDate: dto.examDate ? dto.examDate.slice(0, ISO_DATE_LENGTH) : '',
  description: dto.description ?? '',
});

export const toSessionDto = (state: SessionState): SessionDto => ({
  id: state.id,
  version: state.version,
  name: state.name.trim(),
  examDate: emptyToNull(state.examDate),
  description: emptyToNull(state.description),
});
