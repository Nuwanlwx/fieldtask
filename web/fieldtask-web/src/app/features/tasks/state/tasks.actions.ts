import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { PagedResult } from '../../../core/models/paged-result';
import { TaskListItemDto, TaskStatus } from '../../../core/models/task.models';

// Dispatched by the component in response to a user action or lifecycle event.
export const TasksPageActions = createActionGroup({
  source: 'Tasks Page',
  events: {
    Opened: emptyProps(),
    'Filters Changed': props<{ status: TaskStatus | null; search: string }>(),
    'Page Changed': props<{ page: number }>(),
  },
});

// Dispatched by the effect once the API call settles.
export const TasksApiActions = createActionGroup({
  source: 'Tasks API',
  events: {
    'Tasks Loaded Success': props<{ result: PagedResult<TaskListItemDto> }>(),
    'Tasks Loaded Failure': props<{ error: string }>(),
  },
});
