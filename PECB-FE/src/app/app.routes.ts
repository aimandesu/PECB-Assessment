import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tickets' },
  {
    path: 'tickets',
    title: 'Tickets',
    loadComponent: () =>
      import('./features/tickets/ticket-list/ticket-list').then((m) => m.TicketListComponent),
  },
  {
    path: 'tickets/:id',
    title: 'Ticket details',
    loadComponent: () =>
      import('./features/tickets/ticket-detail/ticket-detail').then((m) => m.TicketDetailComponent),
  },
  { path: '**', redirectTo: 'tickets' },
];
