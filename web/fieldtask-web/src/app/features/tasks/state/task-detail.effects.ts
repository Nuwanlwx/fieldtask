import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, EMPTY, map, of, switchMap, tap, withLatestFrom } from 'rxjs';
import { TaskApiService } from '../../../core/task-api.service';
import { TaskDetailApiActions, TaskDetailPageActions } from './task-detail.actions';
import { taskDetailFeature } from './task-detail.reducer';

@Injectable()
export class TaskDetailEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(TaskApiService);
  private readonly store = inject(Store);
  private readonly router = inject(Router);

  // Opened and Reload Requested both just mean "(re)fetch the current id" —
  // reading it from state (set by the reducer on Opened) instead of the
  // action payload means Reload Requested doesn't need to carry one at all.
  loadTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TaskDetailPageActions.opened, TaskDetailPageActions.reloadRequested),
      withLatestFrom(this.store.select(taskDetailFeature.selectId)),
      switchMap(([, id]) => {
        if (id == null) return EMPTY;
        return this.api.getTask(id).pipe(
          map((task) => TaskDetailApiActions.taskLoadedSuccess({ task })),
          catchError((err: HttpErrorResponse) =>
            of(TaskDetailApiActions.taskLoadedFailure({ error: err.error?.detail ?? 'Failed to load task.' })),
          ),
        );
      }),
    ),
  );

  // The rowVersion that makes optimistic concurrency work comes from the task
  // already sitting in state — exactly what the user last saw on screen —
  // not from a fresh read, which would defeat the whole point of the check.
  assign$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TaskDetailPageActions.assignRequested),
      withLatestFrom(this.store.select(taskDetailFeature.selectTask)),
      switchMap(([{ technicianId }, task]) => {
        if (!task) return EMPTY;
        return this.api.assignTask(task.id, { technicianId, rowVersion: task.rowVersion }).pipe(
          map((updated) => TaskDetailApiActions.assignSuccess({ task: updated })),
          catchError((err: HttpErrorResponse) =>
            of(
              TaskDetailApiActions.assignFailure({
                error: err.error?.detail ?? 'Failed to assign technician.',
                conflict: err.status === 409,
              }),
            ),
          ),
        );
      }),
    ),
  );

  delete$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TaskDetailPageActions.deleteRequested),
      withLatestFrom(this.store.select(taskDetailFeature.selectId)),
      switchMap(([, id]) => {
        if (id == null) return EMPTY;
        return this.api.deleteTask(id).pipe(
          tap(() => this.router.navigate(['/tasks'])),
          map(() => TaskDetailApiActions.deleteSuccess()),
          catchError((err: HttpErrorResponse) =>
            of(
              TaskDetailApiActions.deleteFailure({
                // 403 is already shown as a global banner by the auth interceptor —
                // no need to duplicate it inline on the page too.
                error: err.status === 403 ? null : (err.error?.detail ?? 'Failed to delete task.'),
              }),
            ),
          ),
        );
      }),
    ),
  );
}
