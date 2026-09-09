import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Presentational pager. Paging itself is performed by the server. */
@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="pager" aria-label="Ticket pages">
      <p class="pager__summary">
        Showing <strong>{{ rangeStart() }}-{{ rangeEnd() }}</strong> of
        <strong>{{ total() }}</strong>
      </p>

      <div class="pager__controls">
        <button
          type="button"
          class="btn btn--ghost"
          [disabled]="page() <= 1"
          (click)="goTo(page() - 1)"
        >
          Previous
        </button>

        @for (candidate of pages(); track $index) {
          @if (candidate === null) {
            <span class="pager__gap" aria-hidden="true">...</span>
          } @else {
            <button
              type="button"
              class="pager__page"
              [class.is-current]="candidate === page()"
              [attr.aria-current]="candidate === page() ? 'page' : null"
              (click)="goTo(candidate)"
            >
              {{ candidate }}
            </button>
          }
        }

        <button
          type="button"
          class="btn btn--ghost"
          [disabled]="page() >= lastPage()"
          (click)="goTo(page() + 1)"
        >
          Next
        </button>
      </div>
    </nav>
  `,
  styles: `
    .pager {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.85rem 1rem;
      border-top: 1px solid var(--border);
    }
    .pager__summary { margin: 0; font-size: 0.85rem; color: var(--muted); }
    .pager__controls { display: flex; align-items: center; gap: 0.25rem; flex-wrap: wrap; }
    .pager__gap { padding: 0 0.25rem; color: var(--muted); }
    .pager__page {
      min-width: 2rem;
      height: 2rem;
      padding: 0 0.4rem;
      border: 1px solid var(--border);
      border-radius: 6px;
      background: var(--surface);
      color: var(--text);
      font: inherit;
      font-size: 0.85rem;
      cursor: pointer;
    }
    .pager__page:hover { border-color: var(--accent); }
    .pager__page.is-current {
      background: var(--accent);
      border-color: var(--accent);
      color: #fff;
      font-weight: 600;
    }
  `,
})
export class PaginationComponent {
  readonly page = input.required<number>();
  readonly lastPage = input.required<number>();
  readonly total = input.required<number>();
  readonly pageSize = input.required<number>();

  readonly pageChange = output<number>();

  readonly rangeStart = computed(() =>
    this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.total()));

  /** First page, last page and a window around the current one. `null` renders an ellipsis. */
  readonly pages = computed<(number | null)[]>(() => {
    const last = this.lastPage();
    const current = this.page();
    if (last <= 7) {
      return Array.from({ length: last }, (_, index) => index + 1);
    }

    const window = new Set<number>([1, last, current]);
    for (const offset of [-1, 1]) {
      const candidate = current + offset;
      if (candidate > 1 && candidate < last) {
        window.add(candidate);
      }
    }

    const sorted = [...window].sort((a, b) => a - b);
    const result: (number | null)[] = [];
    let previous = 0;
    for (const value of sorted) {
      if (previous && value - previous > 1) {
        result.push(null);
      }
      result.push(value);
      previous = value;
    }
    return result;
  });

  goTo(page: number): void {
    if (page >= 1 && page <= this.lastPage() && page !== this.page()) {
      this.pageChange.emit(page);
    }
  }
}
