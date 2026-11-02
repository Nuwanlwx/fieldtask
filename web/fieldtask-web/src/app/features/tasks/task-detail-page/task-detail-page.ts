import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthService } from '../../../core/auth/auth.service';
import { TaskDetailPageActions } from '../state/task-detail.actions';
import { taskDetailFeature } from '../state/task-detail.reducer';

@Component({
  imports: [CommonModule, FormsModule, RouterLink],
  selector: 'app-task-detail-page',
  styleUrl: './task-detail-page.scss',
  templateUrl: './task-detail-page.html',
})
export class TaskDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly store = inject(Store);
  protected readonly auth = inject(AuthService);

  readonly task = this.store.selectSignal(taskDetailFeature.selectTask);
  readonly loading = this.store.selectSignal(taskDetailFeature.selectLoading);
  readonly error = this.store.selectSignal(taskDetailFeature.selectError);
  readonly assigning = this.store.selectSignal(taskDetailFeature.selectAssigning);
  readonly assignError = this.store.selectSignal(taskDetailFeature.selectAssignError);
  readonly assignConflict = this.store.selectSignal(taskDetailFeature.selectAssignConflict);
  readonly deleting = this.store.selectSignal(taskDetailFeature.selectDeleting);
  readonly deleteError = this.store.selectSignal(taskDetailFeature.selectDeleteError);

  technicianId: number | null = null;

  constructor() {
    // A subscription, not a one-off snapshot read: if the user navigates from
    // /tasks/3 straight to /tasks/7 (same route, different param), this still
    // fires again. A snapshot taken once in ngOnInit would not.
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const id = Number(params.get('id'));
      this.store.dispatch(TaskDetailPageActions.opened({ id }));
    });
  }

  assign(): void {
    if (this.technicianId == null) return;
    this.store.dispatch(TaskDetailPageActions.assignRequested({ technicianId: this.technicianId }));
  }

  reload(): void {
    this.store.dispatch(TaskDetailPageActions.reloadRequested());
  }

  delete(): void {
    if (!confirm('Delete this task? This cannot be undone.')) return;
    this.store.dispatch(TaskDetailPageActions.deleteRequested());
  }
}
