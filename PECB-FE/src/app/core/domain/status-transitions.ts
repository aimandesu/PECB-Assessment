import { Status } from '../models/ticket.models';

/**
 * Client-side mirror of `Ticket.UpdateStatus` on the server.
 *
 * The server remains the authority — it re-validates every transition and returns
 * `Server.InvalidOperation` on a violation. This table exists so the UI only ever
 * offers transitions that are currently legal.
 */
const TRANSITIONS: Readonly<Record<Status, readonly Status[]>> = {
  New: ['InProgress'],
  InProgress: ['Resolved'],
  Resolved: ['Closed', 'InProgress'],
  Closed: [],
};

export interface TransitionOption {
  target: Status;
  /** True when the transition exists but its precondition is not met yet. */
  blocked: boolean;
  /** Why it is blocked, for display next to the option. */
  reason?: string;
}

/**
 * Transitions reachable from `current`.
 *
 * `New -> InProgress` additionally requires an assigned agent, which is why
 * `hasAssignedAgent` is part of the signature.
 */
export function allowedTransitions(current: Status, hasAssignedAgent: boolean): TransitionOption[] {
  return TRANSITIONS[current].map((target) => {
    const needsAgent = current === 'New' && target === 'InProgress' && !hasAssignedAgent;
    return {
      target,
      blocked: needsAgent,
      reason: needsAgent ? 'Assign an agent before starting work on this ticket.' : undefined,
    };
  });
}

/** A closed ticket is read-only, matching `Ticket.IsReadOnly` on the server. */
export function isReadOnly(status: Status): boolean {
  return status === 'Closed';
}
