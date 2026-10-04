import { Injectable, effect, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';
const KEY = 'logs-platform.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>(this.initial());

  constructor() {
    effect(() => {
      const m = this.mode();
      const cl = document.documentElement.classList;
      cl.toggle('theme-dark', m === 'dark');
      cl.toggle('theme-light', m === 'light');
      try { localStorage.setItem(KEY, m); } catch { /* stockage indisponible */ }
    });
  }

  toggle(): void {
    this.mode.update(m => (m === 'dark' ? 'light' : 'dark'));
  }

  private initial(): ThemeMode {
    try {
      const saved = localStorage.getItem(KEY);
      if (saved === 'light' || saved === 'dark') return saved;
    } catch { /* ignore */ }
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
