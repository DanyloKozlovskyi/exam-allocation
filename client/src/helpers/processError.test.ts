import { AxiosError, AxiosHeaders } from 'axios';
import { ErrorCode } from '../api/dtos';
import { getErrorCode } from './processError';

const axiosError = (data: unknown) =>
  new AxiosError('failed', 'ERR', undefined, undefined, {
    data,
    status: 404,
    statusText: 'Not Found',
    headers: {},
    config: { headers: new AxiosHeaders() },
  });

describe('getErrorCode', () => {
  it('reads ErrorCode from ProblemDetails', () => {
    expect(getErrorCode(axiosError({ code: ErrorCode.SessionNotFound }))).toBe(ErrorCode.SessionNotFound);
  });

  it('falls back to Unknown', () => {
    expect(getErrorCode(new Error('boom'))).toBe(ErrorCode.Unknown);
    expect(getErrorCode(axiosError({ code: 'Nope' }))).toBe(ErrorCode.Unknown);
  });
});
