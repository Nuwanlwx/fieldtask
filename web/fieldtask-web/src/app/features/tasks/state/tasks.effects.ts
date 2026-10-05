import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, map, of, switchMap, withLatestFrom } from 'rxjs';
import { TaskApiService } from '../../../core/task-api.service';
import { TasksApiActions, TasksPageActions } from './tasks.actions';
import { tasksFeature } from './tasks.reducer';

@Injectable()
export class TasksEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(TaskApiService);
  private readonly store = inject(Store);

  // One effect drives every reload: the page opening, a filter change, or a page change.
  // withLatestFrom reads the (already-updated-by-the-reducer) filters/page from the store,
  // so this effect doesn't need to know the shape of each triggering action.
  loadTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksPageActions.opened, TasksPageActions.filtersChanged, TasksPageActions.pageChanged),
      withLatestFrom(
        this.store.select(tasksFeature.selectPage),
        this.store.select(tasksFeature.selectPageSize),
        this.store.select(tasksFeature.selectStatus),
        this.store.select(tasksFeature.selectSearch),
      ),
      switchMap(([, page, pageSize, status, search]) =>
        this.api.getTasks({ page, pageSize, status: status ?? undefined, search: search || undefined }).pipe(
          map((result) => TasksApiActions.tasksLoadedSuccess({ result })),
          catchError((err: HttpErrorResponse) =>
            of(TasksApiActions.tasksLoadedFailure({ error: err.error?.detail ?? 'Failed to load tasks.' })),
          ),
        ),
      ),
    ),
  );
}
