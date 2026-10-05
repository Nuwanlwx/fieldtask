import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { provideMockStore } from '@ngrx/store/testing';
import { of } from 'rxjs';
import { initialTaskDetailState } from '../state/task-detail.state';
import { TaskDetailPage } from './task-detail-page';

describe('TaskDetailPage', () => {
  let fixture: ComponentFixture<TaskDetailPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskDetailPage],
      providers: [
        provideMockStore({ initialState: { taskDetail: initialTaskDetailState } }),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ id: '42' })) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskDetailPage);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });
});
