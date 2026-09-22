import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { HasPermissionDirective } from './has-permission.directive';
import { PermissionService } from '../../core/permissions/permission.service';

@Component({
  standalone: true,
  imports: [HasPermissionDirective],
  template: `<p *appHasPermission="'Patients.View'" data-testid="secure">Secure</p>`
})
class HostComponent {}

describe('HasPermissionDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let permissions: { has: jasmine.Spy };

  beforeEach(async () => {
    permissions = { has: jasmine.createSpy('has') };
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: PermissionService, useValue: permissions }]
    }).compileComponents();
  });

  it('shows content when permission granted', () => {
    permissions.has.and.returnValue(true);
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('[data-testid=secure]'))).toBeTruthy();
  });

  it('hides content when permission denied', () => {
    permissions.has.and.returnValue(false);
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('[data-testid=secure]'))).toBeNull();
  });
});
