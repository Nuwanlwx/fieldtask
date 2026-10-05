import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { TaskStatus } from '../../../core/models/task.models';
import { TasksPageActions } from '../state/tasks.actions';
import { tasksFeature } from '../state/tasks.reducer';

@Component({
  imports: [CommonModule, FormsModule],
  selector: 'app-task-list-page',
  styleUrl: './task-list-page.scss',
  templateUrl: './task-list-page.html',
})
export class TaskListPage implements OnInit {
  private readonly store = inject(Store);

  readonly statuses: TaskStatus[] = ['Open', 'Assigned', 'Done'];

  readonly items = this.store.selectSignal(tasksFeature.selectItems);
  readonly loading = this.store.selectSignal(tasksFeature.selectLoading);
  readonly error = this.store.selectSignal(tasksFeature.selectError);
  readonly page = this.store.selectSignal(tasksFeature.selectPage);
  readonly totalPages = this.store.selectSignal(tasksFeature.selectTotalPages);

  // Local, unsubmitted filter values — the store only updates (and triggers a reload)
  // once the user clicks "Apply", not on every keystroke.
  statusFilter: TaskStatus | '' = '';
  searchFilter = '';

  ngOnInit(): void {
    this.store.dispatch(TasksPageActions.opened());
  }

  applyFilters(): void {
    this.store.dispatch(
      TasksPageActions.filtersChanged({
        status: this.statusFilter || null,
        search: this.searchFilter.trim(),
      }),
    );
  }

  clearFilters(): void {
    this.statusFilter = '';
    this.searchFilter = '';
    this.applyFilters();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.store.dispatch(TasksPageActions.pageChanged({ page }));
  }
}
