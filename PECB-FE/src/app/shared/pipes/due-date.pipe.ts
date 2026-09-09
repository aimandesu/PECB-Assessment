import { Pipe, PipeTransform } from '@angular/core';

/**
 * Renders a due date as a short relative phrase, e.g. `in 3h` or `2d overdue`,
 * so a scan of the list makes urgency obvious without reading timestamps.
 */
@Pipe({ name: 'dueIn' })
export class DueInPipe implements PipeTransform {
  transform(value: string | Date | null | undefined, now: Date = new Date()): string {
    if (!value) {
      return '';
    }

    const due = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(due.getTime())) {
      return '';
    }

    const diffMinutes = Math.round((due.getTime() - now.getTime()) / 60000);
    const overdue = diffMinutes < 0;
    const magnitude = Math.abs(diffMinutes);

    let phrase: string;
    if (magnitude < 1) {
      phrase = 'now';
    } else if (magnitude < 60) {
      phrase = `${magnitude}m`;
    } else if (magnitude < 60 * 24) {
      phrase = `${Math.round(magnitude / 60)}h`;
    } else {
      phrase = `${Math.round(magnitude / (60 * 24))}d`;
    }

    if (phrase === 'now') {
      return overdue ? 'just overdue' : 'due now';
    }
    return overdue ? `${phrase} overdue` : `in ${phrase}`;
  }
}
