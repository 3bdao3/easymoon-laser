import { Permissions } from '../../core/permissions/permissions';
import { NavItem } from './nav-item.model';

export const MAIN_NAV_ITEMS: NavItem[] = [
  {
    labelAr: 'الرئيسية',
    labelEn: 'Home',
    route: '/app/dashboard',
    icon: 'home'
  },
  {
    labelAr: 'العملاء',
    labelEn: 'Customers',
    route: '/app/customers',
    requiredPermission: Permissions.LaserCustomersView,
    icon: 'customers'
  },
  {
    labelAr: 'الحجوزات',
    labelEn: 'Appointments',
    route: '/app/appointments',
    requiredPermission: Permissions.LaserAppointmentsView,
    icon: 'appointments'
  },
  {
    labelAr: 'التقويم',
    labelEn: 'Calendar',
    route: '/app/calendar',
    requiredPermission: Permissions.LaserAppointmentsView,
    icon: 'calendar'
  },
  {
    labelAr: 'حجز جديد',
    labelEn: 'New booking',
    route: '/app/book',
    requiredPermission: Permissions.LaserAppointmentsCreate,
    icon: 'book'
  },
  {
    labelAr: 'المناطق والمدة',
    labelEn: 'Areas & duration',
    route: '/app/laser-services',
    requiredPermission: Permissions.LaserServicesManage,
    icon: 'laser'
  },
  {
    labelAr: 'العروض',
    labelEn: 'Offers',
    route: '/app/offers',
    requiredAnyPermission: [Permissions.LaserOffersView, Permissions.LaserOffersManage],
    icon: 'laser'
  },
  {
    labelAr: 'التقارير',
    labelEn: 'Reports',
    route: '/app/reports',
    requiredPermission: Permissions.LaserReportsView,
    icon: 'reports'
  },
  {
    labelAr: 'الإعدادات',
    labelEn: 'Settings',
    route: '/app/settings',
    requiredPermission: Permissions.LaserSettingsView,
    icon: 'settings'
  }
];
