export type TaskStatus = 'Open' | 'Assigned' | 'Done';

export interface TaskListItemDto {
  id: number;
  title: string;
  status: TaskStatus;
  priority: number;
  createdAt: string;
  technicianId: number | null;
  technicianName: string | null;
}

export interface TaskNoteDto {
  id: number;
  text: string;
  createdAt: string;
}

export interface TaskDetailDto {
  id: number;
  title: string;
  status: TaskStatus;
  priority: number;
  createdAt: string;
  technicianId: number | null;
  technicianName: string | null;
  rowVersion: string;
  notes: TaskNoteDto[];
}

export interface CreateTaskDto {
  title: string;
  priority: number;
}

export interface AssignTaskDto {
  technicianId: number;
  rowVersion: string;
}

export interface SuggestTaskRequestDto {
  complaintText: string;
}

export interface PossibleDuplicateDto {
  taskId: number;
  title: string;
  similarity: number;
}

export interface TaskSuggestionDto {
  title: string;
  priority: number;
  summary: string;
  possibleDuplicates: PossibleDuplicateDto[];
}

