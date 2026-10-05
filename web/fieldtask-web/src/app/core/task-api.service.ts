import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { PagedResult } from './models/paged-result';
import { AssignTaskDto, CreateTaskDto, TaskDetailDto, TaskListItemDto, TaskStatus } from './models/task.models';

export interface GetTasksParams {
  page?: number;
  pageSize?: number;
  status?: TaskStatus;
  search?: string;
}

@Injectable({ providedIn: 'root' })
export class TaskApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/tasks`;

  getTasks(params: GetTasksParams = {}): Observable<PagedResult<TaskListItemDto>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.search) httpParams = httpParams.set('search', params.search);

    return this.http.get<PagedResult<TaskListItemDto>>(this.baseUrl, { params: httpParams });
  }

  getTask(id: number): Observable<TaskDetailDto> {
    return this.http.get<TaskDetailDto>(`${this.baseUrl}/${id}`);
  }

  createTask(dto: CreateTaskDto): Observable<TaskDetailDto> {
    return this.http.post<TaskDetailDto>(this.baseUrl, dto);
  }

  assignTask(id: number, dto: AssignTaskDto): Observable<TaskDetailDto> {
    return this.http.put<TaskDetailDto>(`${this.baseUrl}/${id}/assign`, dto);
  }

  deleteTask(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
