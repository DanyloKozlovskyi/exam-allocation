import { FormFieldError } from '../../../../enums/Sessions';

/** Validation helpers shared by the WS-E forms. Errors are enum values; texts come from `validation.*` in each namespace. */
export const validateText = (value: string, maxLength: number, isRequired: boolean): FormFieldError | undefined => {
  const trimmed = value.trim();
  if (isRequired && !trimmed) {
    return FormFieldError.Required;
  }
  return trimmed.length > maxLength ? FormFieldError.TooLong : undefined;
};

export const validateNumber = (value: string, min: number, max: number, isRequired: boolean): FormFieldError | undefined => {
  const trimmed = value.trim();
  if (!trimmed) {
    return isRequired ? FormFieldError.Required : undefined;
  }
  const parsed = Number(trimmed.replace(',', '.'));
  if (Number.isNaN(parsed)) {
    return FormFieldError.NotANumber;
  }
  return parsed < min || parsed > max ? FormFieldError.OutOfRange : undefined;
};

export const parseNumber = (value: string): number | null => {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }
  const parsed = Number(trimmed.replace(',', '.'));
  return Number.isNaN(parsed) ? null : parsed;
};

export const hasErrors = (errors: object) => Object.values(errors).some(Boolean);

export const emptyToNull = (value: string): string | null => (value.trim() ? value.trim() : null);
