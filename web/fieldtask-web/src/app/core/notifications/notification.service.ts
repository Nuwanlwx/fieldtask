import { Injectable, signal } from '@angular/core';

// A single global message slot, not a queue - this app only ever has one thing
// worth interrupting the user for at a time (e.g. a 403 from the auth interceptor).
@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly message = signal<string | null>(null);

  show(message: string): void {
    this.message.set(message);
  }

  clear(): void {
    this.message.set(null);
  }
}
