import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';
import { Routes } from '@angular/router';
import { TaskDetailEffects } from './state/task-detail.effects';
import { taskDetailFeature } from './state/task-detail.reducer';
import { TasksEffects } from './state/tasks.effects';
import { tasksFeature } from './state/tasks.reducer';
import { TaskDetailPage } from './task-detail-page/task-detail-page';
import { TaskListPage } from './task-list-page/task-list-page';
import { SuggestTaskPage } from './suggest-task-page/suggest-task-page';

export const TASKS_ROUTES: Routes = [
  {
    path: '',
    component: TaskListPage,
    // Registered here, not in app.config, so the tasks slice and its effects
    // only load when this lazy route tree is actually navigated to.
    providers: [provideState(tasksFeature), provideEffects(TasksEffects)],
  },
  {
    // Must come before ':id', or '/tasks/suggest' would be parsed as the id param.
    path: 'suggest',
    component: SuggestTaskPage,
  },
  {
    path: ':id',
    component: TaskDetailPage,
    // Its own feature slice, independent of the list's — the list page and
    // the detail page never need to know about each other's state.
    providers: [provideState(taskDetailFeature), provideEffects(TaskDetailEffects)],
  },
];
