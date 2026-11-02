import { TaskDetailDto } from '../../../core/models/task.models';

export interface TaskDetailState {
  id: number | null;
  task: TaskDetailDto | null;
  loading: boolean;
  error: string | null;

  assigning: boolean;
  assignError: string | null;
  // 409 is distinct from a plain failure: the UI tells the user to reload and
  // retry, rather than just "something went wrong, try again".
  assignConflict: boolean;

  deleting: boolean;
  deleteError: string | null;
}

export const initialTaskDetailState: TaskDetailState = {
  id: null,
  task: null,
  loading: false,
  error: null,
  assigning: false,
  assignError: null,
  assignConflict: false,
  deleting: false,
  deleteError: null,
};
