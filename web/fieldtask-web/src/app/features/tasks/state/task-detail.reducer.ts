import { createFeature, createReducer, on } from '@ngrx/store';
import { TaskDetailApiActions, TaskDetailPageActions } from './task-detail.actions';
import { initialTaskDetailState } from './task-detail.state';

export const taskDetailFeature = createFeature({
  name: 'taskDetail',
  reducer: createReducer(
    initialTaskDetailState,

    on(TaskDetailPageActions.opened, (state, { id }) => ({
      ...initialTaskDetailState, // leaving the previous task's data behind, not just its id
      id,
      loading: true,
    })),

    on(TaskDetailPageActions.reloadRequested, (state) => ({
      ...state,
      loading: true,
      error: null,
    })),

    on(TaskDetailApiActions.taskLoadedSuccess, (state, { task }) => ({
      ...state,
      task,
      loading: false,
      error: null,
    })),

    on(TaskDetailApiActions.taskLoadedFailure, (state, { error }) => ({
      ...state,
      loading: false,
      error,
    })),

    on(TaskDetailPageActions.assignRequested, (state) => ({
      ...state,
      assigning: true,
      assignError: null,
      assignConflict: false,
    })),

    on(TaskDetailApiActions.assignSuccess, (state, { task }) => ({
      ...state,
      task,
      assigning: false,
    })),

    on(TaskDetailApiActions.assignFailure, (state, { error, conflict }) => ({
      ...state,
      assigning: false,
      assignError: error,
      assignConflict: conflict,
    })),

    on(TaskDetailPageActions.deleteRequested, (state) => ({
      ...state,
      deleting: true,
      deleteError: null,
    })),

    on(TaskDetailApiActions.deleteSuccess, (state) => ({
      ...state,
      deleting: false,
    })),

    on(TaskDetailApiActions.deleteFailure, (state, { error }) => ({
      ...state,
      deleting: false,
      deleteError: error,
    })),
  ),
});

export const {
  name: taskDetailFeatureKey,
  reducer: taskDetailReducer,
  selectId,
  selectTask,
  selectLoading,
  selectError,
  selectAssigning,
  selectAssignError,
  selectAssignConflict,
  selectDeleting,
  selectDeleteError,
} = taskDetailFeature;
