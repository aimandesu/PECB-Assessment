import { HttpErrorResponse } from '@angular/common/http';
import { ApiError } from './api-error';

function httpError(status: number, body: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body, url: '/api/ticket/create' });
}

describe('ApiError', () => {
  it('splits a FluentValidation summary into one message per rule', () => {
    const error = ApiError.fromHttp(
      httpError(400, {
        description: 'CreateTicket.Validation',
        customObject: 'Please specify a Title\r\nPlease specify a CustomerEmail',
      }),
    );

    expect(error.code).toBe('CreateTicket.Validation');
    expect(error.messages).toEqual([
      'Please specify a Title',
      'Please specify a CustomerEmail',
    ]);
  });

  it('attributes validation messages to the form controls they belong to', () => {
    const error = ApiError.fromHttp(
      httpError(400, {
        description: 'CreateTicket.Validation',
        customObject: 'Please specify a Title\nPlease specify a CustomerEmail',
      }),
    );

    expect(error.fieldErrors['title']).toEqual(['Please specify a Title']);
    expect(error.fieldErrors['customerEmail']).toEqual(['Please specify a CustomerEmail']);
    expect(error.fieldErrors['description']).toBeUndefined();
  });

  it('flags a domain rule violation raised by the server', () => {
    const error = ApiError.fromHttp(
      httpError(400, {
        description: 'Server.InvalidOperation',
        customObject: 'Invalid status transition from New to Closed.',
      }),
    );

    expect(error.isRuleViolation).toBeTrue();
    expect(error.message).toBe('Invalid status transition from New to Closed.');
  });

  it('reports an unreachable API rather than an empty error', () => {
    const error = ApiError.fromHttp(httpError(0, null));

    expect(error.status).toBe(0);
    expect(error.code).toBe('Network.Unreachable');
    expect(error.messages[0]).toContain('Could not reach the API');
  });
});
