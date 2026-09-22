import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorService } from './api-error.service';

describe('ApiErrorService', () => {
  const service = new ApiErrorService();

  it('prefers problem detail when present', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { detail: 'رقم الهاتف مطلوب', code: 'patients.validation_failed' }
    });
    expect(service.resolveMessage(error)).toBe('رقم الهاتف مطلوب');
  });

  it('maps 401/403/409 to Arabic defaults', () => {
    expect(service.resolveMessage(new HttpErrorResponse({ status: 401 }))).toContain('انتهت جلسة');
    expect(service.resolveMessage(new HttpErrorResponse({ status: 403 }))).toContain('صلاحية');
    expect(service.resolveMessage(new HttpErrorResponse({ status: 409 }))).toContain('تعارضت');
  });

  it('maps status 0 to connection failure message', () => {
    expect(service.resolveMessage(new HttpErrorResponse({ status: 0 }))).toContain('تعذر الاتصال');
  });
});
