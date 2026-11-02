import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../notifications/notification.service';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const notifications = inject(NotificationService);
  const token = auth.getToken();

  const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // A 401 here means the token is missing/expired/invalid - there's no "refresh" step in
      // this project, so the only recovery is logging in again.
      if (err.status === 401) {
        auth.logout();
        router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      } else if (err.status === 403) {
        // A valid, logged-in user whose role just isn't allowed to do this - a policy
        // rejection like this has no ProblemDetails body, so there's nothing to show
        // the user except a generic message.
        notifications.show("You don't have permission to do that.");
      }
      return throwError(() => err);
    }),
  );
};
