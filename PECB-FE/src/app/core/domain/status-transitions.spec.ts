import { allowedTransitions, isReadOnly } from './status-transitions';

describe('status transition rules', () => {
  it('offers only In progress from New, and blocks it until an agent is assigned', () => {
    const withoutAgent = allowedTransitions('New', false);

    expect(withoutAgent.map((option) => option.target)).toEqual(['InProgress']);
    expect(withoutAgent[0].blocked).toBeTrue();
    expect(withoutAgent[0].reason).toContain('Assign an agent');
  });

  it('unblocks New -> In progress once an agent is assigned', () => {
    const withAgent = allowedTransitions('New', true);

    expect(withAgent.map((option) => option.target)).toEqual(['InProgress']);
    expect(withAgent[0].blocked).toBeFalse();
  });

  it('offers only Resolved from In progress', () => {
    const options = allowedTransitions('InProgress', true);

    expect(options.map((option) => option.target)).toEqual(['Resolved']);
    expect(options.every((option) => !option.blocked)).toBeTrue();
  });

  it('allows Resolved to close or to reopen into In progress', () => {
    const options = allowedTransitions('Resolved', true);

    expect(options.map((option) => option.target)).toEqual(['Closed', 'InProgress']);
    expect(options.every((option) => !option.blocked)).toBeTrue();
  });

  it('offers nothing from Closed, which is a terminal, read-only state', () => {
    expect(allowedTransitions('Closed', true)).toEqual([]);
    expect(isReadOnly('Closed')).toBeTrue();
  });

  it('treats every other status as editable', () => {
    expect(isReadOnly('New')).toBeFalse();
    expect(isReadOnly('InProgress')).toBeFalse();
    expect(isReadOnly('Resolved')).toBeFalse();
  });

  it('never offers a transition that skips a step', () => {
    // New must not jump straight to Resolved or Closed.
    expect(allowedTransitions('New', true).map((o) => o.target)).not.toContain('Resolved');
    expect(allowedTransitions('New', true).map((o) => o.target)).not.toContain('Closed');
    // In progress must not close without passing through Resolved.
    expect(allowedTransitions('InProgress', true).map((o) => o.target)).not.toContain('Closed');
  });
});
