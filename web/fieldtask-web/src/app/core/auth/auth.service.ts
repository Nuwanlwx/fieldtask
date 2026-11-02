import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../api-config';
import { LoginRequest, LoginResponse } from './auth.models';

const STORAGE_KEY = 'fieldtask.auth';

interface StoredSession {
  token: string;
  username: string;
  role: string;
}

// Session lives in a signal (so templates/guards react to it) and is mirrored to localStorage
// (so a page refresh doesn't log the user out). The server never tracks sessions - the token
// itself is the only proof of login, so "logging out" here is just forgetting it locally.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = signal<StoredSession | null>(loadSession());

  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly username = computed(() => this.session()?.username ?? null);
  readonly role = computed(() => this.session()?.role ?? null);

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_BASE_URL}/auth/login`, credentials)
      .pipe(tap(result => this.setSession(result)));
  }

  logout(): void {
    this.session.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  getToken(): string | null {
    return this.session()?.token ?? null;
  }

  private setSession(result: LoginResponse): void {
    const session: StoredSession = { token: result.token, username: result.username, role: result.role };
    this.session.set(session);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  }
}

function loadSession(): StoredSession | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as StoredSession;
  } catch {
    return null;
  }
}
