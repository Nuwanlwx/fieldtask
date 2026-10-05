import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { TaskDetailDto } from '../../../core/models/task.models';

export const TaskDetailPageActions = createActionGroup({
  source: 'Task Detail Page',
  events: {
    Opened: props<{ id: number }>(),
    // No payload: the effect reads the id already sitting in state, same
    // trick the tasks-list effect uses for its filters/page.
    'Reload Requested': emptyProps(),
    // Only the technicianId comes from the form — the effect pulls the
    // task's id and rowVersion out of the already-loaded task in state,
    // so the component never has to know what a RowVersion even is.
    'Assign Requested': props<{ technicianId: number }>(),
  },
});

export const TaskDetailApiActions = createActionGroup({
  source: 'Task Detail API',
  events: {
    'Task Loaded Success': props<{ task: TaskDetailDto }>(),
    'Task Loaded Failure': props<{ error: string }>(),
    'Assign Success': props<{ task: TaskDetailDto }>(),
    'Assign Failure': props<{ error: string; conflict: boolean }>(),
  },
});
