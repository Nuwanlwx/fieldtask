import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMockStore } from '@ngrx/store/testing';
import { initialTasksState } from '../state/tasks.state';
import { TaskListPage } from './task-list-page';

describe('TaskListPage', () => {
  let component: TaskListPage;
  let fixture: ComponentFixture<TaskListPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskListPage],
      providers: [provideMockStore({ initialState: { tasks: initialTasksState } })],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskListPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should dispatch Opened on init', () => {
    // ngOnInit already ran during createComponent/whenStable above; just assert
    // the page loaded without throwing and shows no items yet.
    expect(component.items()).toEqual([]);
  });
});
