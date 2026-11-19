import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TaskApiService } from '../../../core/task-api.service';
import { TaskSuggestionDto } from '../../../core/models/task.models';

// Plain standalone component, not an NgRx feature slice: this is a single
// request/response round trip (get a suggestion, confirm, create), not a
// collection with filters/paging - a store slice would be unused machinery.
@Component({
  imports: [CommonModule, FormsModule, RouterLink],
  selector: 'app-suggest-task-page',
  styleUrl: './suggest-task-page.scss',
  templateUrl: './suggest-task-page.html',
})
export class SuggestTaskPage {
  private readonly api = inject(TaskApiService);
  private readonly router = inject(Router);

  complaintText = signal('');
  suggestion = signal<TaskSuggestionDto | null>(null);
  loading = signal(false);
  creating = signal(false);
  error = signal<string | null>(null);

  // Editable copy of the draft - the user can correct the AI's guess before it's ever
  // sent to CreateTask. summary isn't part of CreateTaskDto, it's shown for context only.
  editTitle = signal('');
  editPriority = signal(3);

  getSuggestion(): void {
    if (!this.complaintText().trim()) return;

    this.loading.set(true);
    this.error.set(null);
    this.suggestion.set(null);

    this.api.suggestTask({ complaintText: this.complaintText() }).subscribe({
      next: (suggestion) => {
        this.suggestion.set(suggestion);
        this.editTitle.set(suggestion.title);
        this.editPriority.set(suggestion.priority);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(this.describeSuggestError(err));
        this.loading.set(false);
      },
    });
  }

  // The human-confirm gate: nothing is created until the user explicitly clicks this,
  // using whatever the form holds at that point (the AI draft, possibly edited).
  confirmAndCreate(): void {
    if (!this.suggestion()) return;

    this.creating.set(true);
    this.error.set(null);

    this.api.createTask({ title: this.editTitle(), priority: this.editPriority() }).subscribe({
      next: (task) => this.router.navigate(['/tasks', task.id]),
      error: () => {
        this.error.set('Could not create the task.');
        this.creating.set(false);
      },
    });
  }

  discardSuggestion(): void {
    this.suggestion.set(null);
  }
  private describeSuggestError(err: HttpErrorResponse): string {
    if (err.status === 429) {
      const retryAfter = Number(err.headers.get('Retry-After'));
      return retryAfter > 0
        ? `You've reached the suggestion limit. Try again in ${retryAfter} seconds.`
        : "You've reached the suggestion limit. Try again shortly.";
    }
    return 'Could not get a suggestion. Try again.';
  }
}
