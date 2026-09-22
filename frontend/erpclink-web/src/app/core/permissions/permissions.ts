/** Mirrors backend PermissionCodes string values exactly. */
export const Permissions = {
  AdministrationUsersView: 'Administration.Users.View',
  AdministrationUsersCreate: 'Administration.Users.Create',
  AdministrationUsersUpdate: 'Administration.Users.Update',
  AdministrationUsersDelete: 'Administration.Users.Delete',
  AdministrationRolesManage: 'Administration.Roles.Manage',
  AdministrationPermissionsView: 'Administration.Permissions.View',

  // Laser Clinic
  LaserCustomersView: 'LaserClinic.Customers.View',
  LaserCustomersCreate: 'LaserClinic.Customers.Create',
  LaserCustomersUpdate: 'LaserClinic.Customers.Update',
  LaserServicesView: 'LaserClinic.Services.View',
  LaserServicesManage: 'LaserClinic.Services.Manage',
  LaserOffersView: 'LaserClinic.Offers.View',
  LaserOffersManage: 'LaserClinic.Offers.Manage',
  LaserAppointmentsView: 'LaserClinic.Appointments.View',
  LaserAppointmentsCreate: 'LaserClinic.Appointments.Create',
  LaserAppointmentsUpdate: 'LaserClinic.Appointments.Update',
  LaserAppointmentsCancel: 'LaserClinic.Appointments.Cancel',
  LaserSettingsView: 'LaserClinic.Settings.View',
  LaserSettingsManage: 'LaserClinic.Settings.Manage',
  LaserDashboardView: 'LaserClinic.Dashboard.View',
  LaserReportsView: 'LaserClinic.Reports.View'
} as const;

export type PermissionCode = (typeof Permissions)[keyof typeof Permissions];
