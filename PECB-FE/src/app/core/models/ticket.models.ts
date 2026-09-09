/**
 * Enums are serialised as strings by the backend (`JsonStringEnumConverter` for
 * request/response bodies, `Mapster` `.ToString()` for the ticket DTO), so the
 * client models them as string literal unions rather than numbers.
 */

export const STATUSES = ['New', 'InProgress', 'Resolved', 'Closed'] as const;
export type Status = (typeof STATUSES)[number];

export const PRIORITIES = ['Low', 'Normal', 'High', 'Critical'] as const;
export type Priority = (typeof PRIORITIES)[number];

export type AssignerOption = 'Assign' | 'Unassign';

export interface TicketComment {
  id: string;
  authorName: string;
  body: string;
  createdDate: string;
}

/** Mirrors `TicketDto`. */
export interface Ticket {
  id: string;
  reference: string;
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: Priority;
  status: Status;
  assignedAgent: string | null;
  createdDate: string;
  lastModifiedDate: string;
  dueDate: string;
  resolvedDate: string | null;
  closedDate: string | null;
  isOverdue: boolean;
  comments: TicketComment[];
}

/** Criteria the list page sends to the server. Filtering is server-side. */
export interface TicketQuery {
  page: number;
  pageSize: number;
  /** Free-text term matched against reference, title and customer name. */
  search: string;
  status: Status | null;
  priority: Priority | null;
  agentId: string | null;
  overdueOnly: boolean;
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: Priority;
}

export interface UpdateTicketRequest extends CreateTicketRequest {
  id: string;
  status: Status;
}

/** Field length limits, kept in sync with the `[MaxLength]` attributes on the entities. */
export const TICKET_LIMITS = {
  title: 100,
  description: 250,
  customerName: 50,
  customerEmail: 250,
  commentAuthor: 100,
  commentBody: 200,
} as const;

export function statusLabel(status: Status): string {
  return status === 'InProgress' ? 'In progress' : status;
}
