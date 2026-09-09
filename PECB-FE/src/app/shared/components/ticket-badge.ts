import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Priority, Status, statusLabel } from '../../core/models/ticket.models';

/** Colour-coded pill for a ticket's status or priority. */
@Component({
  selector: 'app-ticket-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [class]="'badge--' + slug()">{{ label() }}</span>`,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      padding: 0.15rem 0.55rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 600;
      white-space: nowrap;
      border: 1px solid transparent;
    }
    .badge--new { background: #eef2ff; color: #3730a3; border-color: #c7d2fe; }
    .badge--inprogress { background: #ecfeff; color: #155e75; border-color: #a5f3fc; }
    .badge--resolved { background: #ecfdf5; color: #065f46; border-color: #a7f3d0; }
    .badge--closed { background: #f1f5f9; color: #475569; border-color: #cbd5e1; }
    .badge--low { background: #f8fafc; color: #64748b; border-color: #e2e8f0; }
    .badge--normal { background: #eff6ff; color: #1d4ed8; border-color: #bfdbfe; }
    .badge--high { background: #fff7ed; color: #c2410c; border-color: #fed7aa; }
    .badge--critical { background: #fef2f2; color: #b91c1c; border-color: #fecaca; }
  `,
})
export class TicketBadgeComponent {
  readonly value = input.required<Status | Priority>();

  readonly slug = computed(() => this.value().toLowerCase());
  readonly label = computed(() => {
    const value = this.value();
    return value === 'InProgress' ? statusLabel(value) : value;
  });
}
