import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  imports: [CommonModule, FormsModule],
  selector: 'app-login-page',
  styleUrl: './login-page.scss',
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  username = '';
  password = '';
  readonly error = signal<string | null>(null);
  readonly submitting = signal(false);

  submit(): void {
    this.error.set(null);
    this.submitting.set(true);

    this.auth.login({ username: this.username, password: this.password }).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/tasks';
        this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.error.set('Invalid username or password.');
        this.submitting.set(false);
      },
    });
  }
}
