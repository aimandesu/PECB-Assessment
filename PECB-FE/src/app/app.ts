import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ToastHostComponent } from './shared/components/toast-host';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, ToastHostComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
