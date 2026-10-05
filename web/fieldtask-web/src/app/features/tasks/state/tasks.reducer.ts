import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { TasksApiActions, TasksPageActions } from './tasks.actions';
import { initialTasksState } from './tasks.state';

export const tasksFeature = createFeature({
  name: 'tasks',
  reducer: createReducer(
    initialTasksState,

    on(TasksPageActions.opened, (state) => ({ ...state, loading: true, error: null })),

    // A filter change always jumps back to page 1 — a filtered result set is shorter,
    // so staying on e.g. page 5 would likely land past the end of it.
    on(TasksPageActions.filtersChanged, (state, { status, search }) => ({
      ...state,
      status,
      search,
      page: 1,
      loading: true,
      error: null,
    })),

    on(TasksPageActions.pageChanged, (state, { page }) => ({
      ...state,
      page,
      loading: true,
      error: null,
    })),

    on(TasksApiActions.tasksLoadedSuccess, (state, { result }) => ({
      ...state,
      items: result.items,
      total: result.total,
      page: result.page,
      pageSize: result.pageSize,
      loading: false,
      error: null,
    })),

    on(TasksApiActions.tasksLoadedFailure, (state, { error }) => ({
      ...state,
      loading: false,
      error,
    })),
  ),
  extraSelectors: ({ selectTotal, selectPageSize }) => ({
    selectTotalPages: createSelector(selectTotal, selectPageSize, (total, pageSize) =>
      pageSize > 0 ? Math.ceil(total / pageSize) : 0,
    ),
  }),
});

export const {
  name: tasksFeatureKey,
  reducer: tasksReducer,
  selectItems,
  selectTotal,
  selectPage,
  selectPageSize,
  selectStatus,
  selectSearch,
  selectLoading,
  selectError,
  selectTotalPages,
} = tasksFeature;
