import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ProblemDetails } from '../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  resolveMessage(error: HttpErrorResponse): string {
    const problem = this.asProblemDetails(error);
    const detail = problem.detail?.trim();
    if (detail) {
      return detail;
    }

    switch (error.status) {
      case 0:
        return 'تعذر الاتصال بالخادم. تأكد أن الـ API يعمل على http://localhost:5284 وأن Angular يعمل عبر npm start.';
      case 400:
        return 'طلب غير صالح. تحقق من البيانات المدخلة.';
      case 401:
        return 'انتهت جلسة الدخول، برجاء تسجيل الدخول مرة أخرى.';
      case 403:
        return 'ليس لديك صلاحية لتنفيذ هذا الإجراء.';
      case 404:
        return 'العنصر المطلوب غير موجود.';
      case 409:
        return 'العملية تعارضت مع تغيير آخر. برجاء تحديث البيانات والمحاولة مرة أخرى.';
      case 422:
        return 'تعذر معالجة البيانات المرسلة.';
      case 500:
        return 'حدث خطأ في الخادم. يرجى المحاولة لاحقاً.';
      default:
        return error.message || 'حدث خطأ غير متوقع.';
    }
  }

  private asProblemDetails(error: HttpErrorResponse): ProblemDetails {
    const body = error.error;
    if (body && typeof body === 'object') {
      return body as ProblemDetails;
    }
    return {};
  }
}
