import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { httpErrorInterceptor } from '../../../core/http/http-error.interceptor';
import { Ticket } from '../../../core/models/ticket.models';
import { TicketListComponent } from './ticket-list';

function makeTicket(overrides: Partial<Ticket> = {}): Ticket {
  return {
    id: 'ticket-1',
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
    isOverdue: false,
    comments: [],
    ...overrides,
  };
}

function page(rows: Ticket[], total = rows.length) {
  return { data: rows, currentPage: 1, pageSize: 10, total, lastPage: Math.ceil(total / 10) };
}

describe('TicketListComponent', () => {
  let fixture: ComponentFixture<TicketListComponent>;
  let httpMock: HttpTestingController;

  /** Answers the agent roster request the page issues on construction. */
  function flushAgents(): void {
    httpMock
      .expectOne((request) => request.url.endsWith('/agent/get-all'))
      .flush({ data: [{ id: 'agent-1', fullName: 'Dana Reyes', department: 'Technical', active: true }] });
  }

  function expectListRequest() {
    return httpMock.expectOne((request) => request.url.endsWith('/ticket/get-all'));
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [TicketListComponent],
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    fixture = TestBed.createComponent(TicketListComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('shows a loading state, then renders one row per ticket returned by the server', async () => {
    flushAgents();

    expect(fixture.nativeElement.querySelector('app-spinner')).not.toBeNull();

    expectListRequest().flush(
      page([makeTicket(), makeTicket({ id: 'ticket-2', reference: 'TCK-2026-0002' })]),
    );
    await fixture.whenStable();
    fixture.detectChanges();

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(rows.length).toBe(2);
    expect(fixture.nativeElement.querySelector('app-spinner')).toBeNull();
  });

  it('highlights overdue tickets and leaves the others unmarked', async () => {
    flushAgents();
    expectListRequest().flush(
      page([
        makeTicket({ id: 'ticket-1', isOverdue: true }),
        makeTicket({ id: 'ticket-2', isOverdue: false }),
      ]),
    );
    await fixture.whenStable();
    fixture.detectChanges();

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(rows[0].nativeElement.classList).toContain('is-overdue');
    expect(rows[1].nativeElement.classList).not.toContain('is-overdue');
    expect(rows[0].nativeElement.querySelector('.overdue-flag')).not.toBeNull();
  });

  it('renders an empty state when the server returns no tickets', async () => {
    flushAgents();
    expectListRequest().flush(page([]));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-empty-state')).not.toBeNull();
    expect(fixture.debugElement.queryAll(By.css('tbody tr')).length).toBe(0);
  });

  it('debounces the search box and asks the server to do the filtering', async () => {
    flushAgents();
    expectListRequest().flush(page([makeTicket()]));
    await fixture.whenStable();
    fixture.detectChanges();

    const input: HTMLInputElement = fixture.nativeElement.querySelector('#ticket-search');

    // Three keystrokes in quick succession must not produce three requests.
    for (const term of ['a', 'ac', 'acme']) {
      input.value = term;
      input.dispatchEvent(new Event('input'));
    }
    httpMock.expectNone((request) => request.url.endsWith('/ticket/get-all'));

    await new Promise((resolve) => setTimeout(resolve, 500));
    await fixture.whenStable();

    const request = expectListRequest();
    expect(request.request.params.get('search')).toBe('acme');
    // A new term always returns the user to the first page.
    expect(request.request.params.get('pageNumber')).toBe('1');

    request.flush(page([makeTicket()]));
    await fixture.whenStable();
  });

  it('sends a filter change to the server as a query parameter', async () => {
    flushAgents();
    expectListRequest().flush(page([makeTicket()]));
    await fixture.whenStable();
    fixture.detectChanges();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#filter-status');
    select.value = 'InProgress';
    select.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    const request = expectListRequest();
    expect(request.request.params.get('status')).toBe('InProgress');

    request.flush(page([]));
    await fixture.whenStable();
  });

  it('surfaces a failed list request instead of showing an empty table', async () => {
    flushAgents();
    expectListRequest().flush(
      { description: 'Server.Error', customObject: 'Something broke.' },
      { status: 500, statusText: 'Server Error' },
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-error-summary')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Something broke.');
  });
});
