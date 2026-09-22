export interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string | null;
  roles?: string[] | null;
}

export interface UpdateUserRequest {
  fullName: string;
  phoneNumber?: string | null;
  email?: string | null;
}

export interface UserDto {
  id: string;
  email: string;
  userName: string | null;
  fullName: string;
  phoneNumber: string | null;
  isActive: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
  roles: string[];
}
