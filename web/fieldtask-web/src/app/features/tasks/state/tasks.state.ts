import { TaskListItemDto, TaskStatus } from '../../../core/models/task.models';

export interface TasksState {
  items: TaskListItemDto[];
  total: number;
  page: number;
  pageSize: number;
  status: TaskStatus | null;
  search: string;
  loading: boolean;
  error: string | null;
}

export const initialTasksState: TasksState = {
  items: [],
  total: 0,
  page: 1,
  pageSize: 10,
  status: null,
  search: '',
  loading: false,
  error: null,
};
