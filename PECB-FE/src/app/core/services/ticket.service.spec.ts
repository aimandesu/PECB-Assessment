import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiError } from '../http/api-error';
import { httpErrorInterceptor } from '../http/http-error.interceptor';
import { Ticket, TicketQuery } from '../models/ticket.models';
import { TicketService } from './ticket.service';

const ticket: Ticket = {
  id: '3f1c9a1e-0000-4000-8000-000000000001',
  reference: 'TCK-2026-0001',
  title: 'Cannot log in',
  description: 'The portal rejects valid credentials.',
  customerName: 'Acme Ltd',
  customerEmail: 'ops@acme.test',
  priority: 'High',
  status: 'New',
  assignedAgent: null,
  createdDate: '2026-09-01T08:00:00Z',
  lastModifiedDate: '2026-09-01T08:00:00Z',
  dueDate: '2026-09-02T08:00:00Z',
  resolvedDate: null,
  closedDate: null,
  isOverdue: true,
  comments: [],
};

const baseQuery: TicketQuery = {
  page: 1,
  pageSize: 10,
  search: '',
  status: null,
  priority: null,
  agentId: null,
  overdueOnly: false,
};

describe('TicketService', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sends every search and filter criterion to the server as query parameters', () => {
    service
      .list({
        ...baseQuery,
        page: 3,
        pageSize: 25,
        search: '  acme  ',
        status: 'InProgress',
        priority: 'Critical',
        agentId: 'agent-1',
        overdueOnly: true,
      })
      .subscribe();

    const request = httpMock.expectOne((candidate) =>
      candidate.url.endsWith('/ticket/get-all'),
    );

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('3');
    expect(request.request.params.get('pageSize')).toBe('25');
    // Whitespace is trimmed so the same term never produces two distinct requests.
    expect(request.request.params.get('search')).toBe('acme');
    expect(request.request.params.get('status')).toBe('InProgress');
    expect(request.request.params.get('priority')).toBe('Critical');
    expect(request.request.params.get('agentId')).toBe('agent-1');
    expect(request.request.params.get('isTicketOverdue')).toBe('true');

    request.flush({ data: [ticket], currentPage: 3, pageSize: 25, total: 51, lastPage: 3 });
  });

  it('omits criteria that are not set, rather than sending empty values', () => {
    service.list(baseQuery).subscribe();

    const request = httpMock.expectOne((candidate) =>
      candidate.url.endsWith('/ticket/get-all'),
    );

    expect(request.request.params.has('search')).toBeFalse();
    expect(request.request.params.has('status')).toBeFalse();
    expect(request.request.params.has('priority')).toBeFalse();
    expect(request.request.params.has('agentId')).toBeFalse();
    expect(request.request.params.has('isTicketOverdue')).toBeFalse();

    request.flush({ data: [], currentPage: 1, pageSize: 10, total: 0, lastPage: 0 });
  });

  it('unwraps the ResultResponse envelope when reading one ticket', () => {
    let received: Ticket | null | undefined;
    service.get(ticket.id).subscribe((value) => (received = value));

    const request = httpMock.expectOne((candidate) => candidate.url.endsWith('/ticket/get'));
    expect(request.request.params.get('ticketId')).toBe(ticket.id);

    request.flush({ data: ticket });

    expect(received).toEqual(ticket);
  });

  it('treats a null payload on HTTP 200 as "not found"', () => {
    let received: Ticket | null | undefined = ticket;
    service.get(ticket.id).subscribe((value) => (received = value));

    httpMock
      .expectOne((candidate) => candidate.url.endsWith('/ticket/get'))
      .flush({ data: null, description: 'Ticket not found.' });

    expect(received).toBeNull();
  });

  it('surfaces a rejected status transition as a rule violation', () => {
    let failure: ApiError | undefined;
    service.changeStatus(ticket.id, 'Closed').subscribe({
      error: (error: ApiError) => (failure = error),
    });

    const request = httpMock.expectOne((candidate) => candidate.url.endsWith('/ticket/status'));
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ ticketId: ticket.id, status: 'Closed' });

    request.flush(
      {
        description: 'Server.InvalidOperation',
        customObject: 'Invalid status transition from New to Closed.',
      },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(failure).toBeInstanceOf(ApiError);
    expect(failure!.isRuleViolation).toBeTrue();
    expect(failure!.message).toContain('Invalid status transition');
  });

  it('sends the currently assigned agent back when unassigning', () => {
    service.setAgent(ticket.id, 'agent-1', 'Unassign').subscribe();

    const request = httpMock.expectOne((candidate) => candidate.url.endsWith('/ticket/assigner'));
    expect(request.request.body).toEqual({
      ticketId: ticket.id,
      agentId: 'agent-1',
      assignerOption: 'Unassign',
    });

    request.flush({ data: ticket });
  });
});
