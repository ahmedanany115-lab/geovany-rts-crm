export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl: string | null;
  roles: string[];
  permissions: string[];
}

export interface AuthResponseDto {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken?: string;   // returned in body for cross-origin use
  user: UserDto;
}

export interface LoginRequest {
  email: string;
  password: string;
}
