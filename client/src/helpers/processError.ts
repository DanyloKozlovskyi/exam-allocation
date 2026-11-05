import axios from 'axios';
import type { TFunction } from 'i18next';
import { ErrorCode, type ProblemDetails } from '../api/dtos';

export interface ProblemDetailsWithCode extends ProblemDetails {
  code?: ErrorCode;
}

const knownCodes = new Set<string>(Object.values(ErrorCode));

export const getErrorCode = (error: unknown): ErrorCode => {
  if (axios.isAxiosError<ProblemDetailsWithCode>(error)) {
    const code = error.response?.data?.code;
    if (code && knownCodes.has(code)) {
      return code;
    }
  }

  return ErrorCode.Unknown;
};

export const processError = (error: unknown, t: TFunction, notify: (message: string) => void) => {
  notify(t(`errors.${getErrorCode(error)}`));
};
