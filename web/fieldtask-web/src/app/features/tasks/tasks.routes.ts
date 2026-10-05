import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';
import { Routes } from '@angular/router';
import { TasksEffects } from './state/tasks.effects';
import { tasksFeature } from './state/tasks.reducer';
import { TaskListPage } from './task-list-page/task-list-page';

export const TASKS_ROUTES: Routes = [
  {
    path: '',
    component: TaskListPage,
    // Registered here, not in app.config, so the tasks slice and its effects
    // only load when this lazy route tree is actually navigated to.
    providers: [provideState(tasksFeature), provideEffects(TasksEffects)],
  },
];
